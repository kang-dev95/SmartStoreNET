SmartStore.Admin.Media = (function () {
	function FileConflictResolutionDialog() {
		var self = this;

		// Private variables.
		var _url = $('head > meta[property="sm:root"]').attr('content') + 'admin/media/fileconflictresolutiondialog';
		var _dialog = null;
		var _dupeFileDisplay = null;

		// Public variables.
		Object.defineProperty(this, 'currentConflict', {
			get: function () {
				return this.queue ? this.queue[this.currentIndex] : null;
			}
		});

		Object.defineProperty(this, 'isOpen', {
			get: function () { return $(_dialog).hasClass("show"); }
		});

		Object.defineProperty(this, 'resolutionType', {
			get: function () {
				if (!_dialog)
					return undefined;

				return parseInt(_dialog.find('input[name=resolution-type]:checked').val());
			}
		});

		// Public functions.
		this.open = function (opts) {
			if (this.isOpen)
				return;

			// Public variables.
			this.currentIndex = 0;
			this.callerId = opts.callerId;
			this.isSingleFileUpload = opts.isSingleFileUpload;
			this.queue = opts.queue;
			this.onResolve = opts.onResolve || _.noop; // return params => self, dupeFileHandlingType, saveSelection, files
			this.onComplete = opts.onComplete || _.noop; // return params => self, isCanceled
			this.closeOnCompleted = toBool(opts.closeOnCompleted, true);

			if (this.queue && this.queue.length) {
				ensureDialog(function () {
					this.modal('show');
					self.refresh();
				});
			}
		};

		this.close = function () {
			if (_dialog && _dialog.length) {
				_dialog.modal('hide');
			}
		};

		this.next = function () {
			if (!this.isOpen)
				return;

			this.currentIndex++;

			var conflict = this.currentConflict;
			if (conflict) {
				this.refresh(conflict);
			}
			else {
				// End of queue is reached.
				if (this.closeOnCompleted) {
					this.close();
				}
				else {
					if (_.isFunction(this.onComplete))
						this.onComplete.apply(this, [false]);
                }
			}
		};

		this.refresh = function (conflict) {
			conflict = conflict || this.currentConflict;
			if (!conflict)
				return;

			// Enable apply button.
			_dialog.find(".btn-apply").removeClass("disabled");

			var existingFileDisplay = _dialog.find(".existing-file-display");
			var source = conflict.source;
			var dest = conflict.dest;

			// Display current filename in intro text.
			_dialog.find(".intro .current-file").html('<b class="fwm">' + source.name + '</b>');

			// Display remaining file count.
			_dialog.find(".remaining-file-counter .current-count").text(this.currentIndex + 1);
			_dialog.find(".remaining-file-counter .total-count").text(this.queue.length);

			refreshFileDisplay(_dupeFileDisplay, source);
			refreshFileDisplay(existingFileDisplay, dest);

			// Trigger change to display changed filename immediately.
			$("input[name=resolution-type]:checked").trigger("change");
		};

		// Private functions.
		var refreshFileDisplay = function (el, file) {
			var preview = SmartStore.media.getPreview(file, { iconCssClasses: "fa-4x" });
			el.find(".file-preview").html(preview.thumbHtml);
			SmartStore.media.lazyLoadThumbnails(el);

			el.find(".file-name").text(file.name);
			el.find(".file-name").attr("title", file.name);
			//el.find(".file-date").text(moment(file.createdOn).format('L LTS'));
			el.find(".file-size").text(_.formatFileSize(file.size));

			if (file.dimensions) {
				var width = parseInt(file.dimensions.split(",")[0]);
				var height = parseInt(file.dimensions.split(",")[1]);

				if (width && height) {
					el.find(".file-dimensions").text(width + " x " + height);
				}
			}
		};

		function ensureDialog(onReady) {
			if (!_dialog || !_dialog.length) {
				_dialog = $("#duplicate-window");
			}

			if (_dialog.length) {
				_dupeFileDisplay = _dialog.find(".dupe-file-display");
				onReady.apply(_dialog);
				return;
			}

			// Get dialog via ajax and append to body.
			$.ajax({
				async: true,
				cache: false,
				type: 'POST',
				url: _url,
				success: function (response) {
					$("body").append($(response));
					_dialog = $("#duplicate-window");
					_dupeFileDisplay = _dialog.find(".dupe-file-display");

					if (self.isSingleFileUpload) {
						_dialog.find("#apply-to-remaining").parent().hide();
						_dialog.find(".remaining-file-counter").hide();
					}

					// Listen to change events of radio group (dupe handling type) and display name of renamed file accordingly.
					$(_dialog).on("change", 'input[name=resolution-type]', function (e) {
						var fileName = self.currentConflict.dest.name;

						if ($(e.target).val() === "2") {
							var uniquePath = self.currentConflict.dest.uniquePath;
							fileName = uniquePath.substr(uniquePath.lastIndexOf("/") + 1);
						}

						_dupeFileDisplay
							.find(".file-name")
							.attr("title", fileName)
							.text(fileName);
					});

					$(_dialog).on("click", ".btn-apply", function () {
						_dialog.data('canceled', false);
						var applyToRemaining = _dialog.find('#apply-to-remaining').is(":checked");

						// Display apply button until current item is processed & next item is called by refresh (prevents double clicks while the server is still busy).
						$(this).addClass("disabled");

						if (_.isFunction(self.onResolve)) {
							var start = self.currentIndex;
							var end = applyToRemaining ? self.queue.length : self.currentIndex + 1;
							var slice = self.queue.slice(start, end);
							if (applyToRemaining) {
								self.currentIndex = self.queue.length - 1;
							}

							// Set file status for later access.
							for (var i in slice) {
								slice[i].resolutionType = self.resolutionType;
							}

							self.onResolve.apply(self, [self.resolutionType, slice]);
						}
						return false;
					});

					$(_dialog).on("click", ".btn-cancel", function () {
						_dialog.data('canceled', true);
						self.queue = null;
						self.close();
						return false;
					});

					$(_dialog).on("hidden.bs.modal", function () {
						if (_.isFunction(self.onComplete)) {
							self.onComplete.apply(self, [_dialog.data('canceled')]);
                        }

						_dialog.trigger("resolution-complete");

						self.currentIndex = 0;
						self.callerId = null;
						self.queue = null;
						self.onResolve = _.noop;
						self.onComplete = _.noop;
					});

					onReady.apply(_dialog);
				}
			});
		};
	};

	return {
		convertDropzoneFileQueue: function (queue) {
			return _.map(queue, function (dzfile) {
				var idx = dzfile.name.lastIndexOf('.');
				var title = idx > -1 ? dzfile.name.substring(0, idx) : dzfile.name;
				var ext = idx > -1 ? dzfile.name.substring(idx) : '';

				// Temp stub for resolving media type only
				var stub = { ext: ext, mime: dzfile.type };
				var mediaType = SmartStore.media.getIconHint(stub).mediaType;

				var file = {
					thumbUrl: dzfile.dataURL ? dzfile.dataURL : null,
					name: dzfile.name,
					title: title,
					ext: ext,
					mime: dzfile.type,
					type: mediaType,
					createdOn: dzfile.lastModifiedDate,
					size: dzfile.size,
					width: dzfile.width ? dzfile.width : null,
					height: dzfile.height ? dzfile.height : null,
					dimensions: dzfile.width && dzfile.height ? dzfile.width + ", " + dzfile.height : null
				};

				return { source: file, dest: dzfile.media, original: dzfile };
			});
		},
		fileConflictResolutionDialog: new FileConflictResolutionDialog()
	};
})();