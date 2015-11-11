

    var ctrl = function () {
        this.initFileUpload();
        this.initVideo();
    };

    ctrl.prototype.initFileUpload = function () {
        
        this.resetIndex();

        $('#fileupload').fileupload({
            dataType: 'json',
            maxChunkSize: 1000000,
            add: this.onFileAdd.bind(this),
            formData: this.getFormData.bind(this),
            done: this.fileUploadDone.bind(this)
        });
    };

    ctrl.prototype.onFileAdd = function (e, data) {
        if (data.files != null && data.files[0] != null) {
            //this.filename = data.files[0].name;
            //this.extension = data.files[0].name.substr(data.files[0].name.lastIndexOf('.') + 1);
            data.submit();
        }
        return false;
    };

    ctrl.prototype.getFormData = function () {
        this.currentChunkIndex++;
        return [{
            name: 'segment',
            value: this.currentChunkIndex
        }]
    };

    ctrl.prototype.fileUploadDone = function (e, data) {
        if (data.result.Success) {

            $.ajax({
                url: 'home/uploaddone',
                data: {
                    segmentsCount: this.currentChunkIndex,
                    fileName: data.result.FileName,
                    contentType: data.result.ContentType
                },
                dataType: 'json',
                success: function (response) {
                    console.log(response);
                }
            })

        }

        this.resetIndex();
    };

    ctrl.prototype.resetIndex = function () {
        this.currentChunkIndex = -1;
    };

    ctrl.prototype.initVideo = function () {
        var $video = $('#videoPlayer');

        if ($video.length) {

            var videoPlayer = videojs($video.get(0), {
                controls: true,
                preload: 'none',
                width: 640,
                height: 360,
            }, function () {
            });
        }
    };

    $(function () {

        var handler = new ctrl();

    });
