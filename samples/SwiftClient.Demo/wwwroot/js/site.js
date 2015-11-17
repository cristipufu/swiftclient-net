
var ctrl = function () {
    this.initFileUpload();
    this.initTree();
    this.initVideo();
};

ctrl.prototype.initTree = function () {
    $.Mustache.addFromDom();

    this.renderTree($('#tree').data('tree'));

    $('#tree').on('click', '.js-treeToggle', function (e) {
        e.preventDefault();
        var $target = $(e.currentTarget),
            $ul = $target.siblings('ul');

        if ($ul.is(":visible")) {
            $ul.hide();
            $target.addClass('glyphicon-plus').removeClass('glyphicon-minus');
        } else {
            $ul.show();
            $target.addClass('glyphicon-minus').removeClass('glyphicon-plus');
        }
    });
};

ctrl.prototype.renderTree = function (data) {
    var $html = $.Mustache.render('tree-template', data);
    $('#tree').html($html);
};

ctrl.prototype.refreshTree = function () {
    $.ajax({
        url: 'home/refreshtree',
        data: {
        },
        dataType: 'json',
        success: (function (response) {
            if (response.Data) {
                this.renderTree(response.Data);
                $('.js-uploadStatus').html('');
            }
        }).bind(this)
    });
};

ctrl.prototype.initFileUpload = function () {

    this.resetIndex();

    $('#fileupload').fileupload({
        dataType: 'json',
        maxChunkSize: 2000000,
        add: this.onFileAdd.bind(this),
        progress: this.uploadProgress.bind(this),
        formData: this.getFormData.bind(this),
        done: this.fileUploadDone.bind(this)
    });
};

ctrl.prototype.onFileAdd = function (e, data) {
    if (data.files != null && data.files[0] != null) {
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

ctrl.prototype.uploadProgress = function (e, data) {
    var progress = (data.loaded / data.total * 100).toFixed(2) + '%';
    $('.js-uploadStatus').html('Uploaded ' + progress + ' (' + this.formatSize(data.loaded) + ' of ' + this.formatSize(data.total) + ')');
};

ctrl.prototype.formatSize = function (bytes) {
    if (typeof bytes !== 'number') {
        return '';
    }
    if (bytes >= 1000000000) {
        return (bytes / 1000000000).toFixed(2) + ' GB';
    }
    if (bytes >= 1000000) {
        return (bytes / 1000000).toFixed(2) + ' MB';
    }
    return (bytes / 1000).toFixed(2) + ' KB';
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
            success: (function (response) {
                $('.js-uploadStatus').html('Upload done');
                this.refreshTree();
            }).bind(this)
        });
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
