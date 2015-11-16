
var ctrl = function () {
    this.initFileUpload();
    this.initTree();
    this.initVideo();
};

ctrl.prototype.initTree = function () {
    $.Mustache.addFromDom();

    var treeData = this.getTreeData();
    var $html = $.Mustache.render('tree-template', treeData);

    $('#tree').append($html);

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

ctrl.prototype.getTreeData = function () {
    return $('#tree').data('tree');
};

ctrl.prototype.initFileUpload = function () {

    this.resetIndex();

    $('#fileupload').fileupload({
        dataType: 'json',
        maxChunkSize: 2000000,
        add: this.onFileAdd.bind(this),
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
