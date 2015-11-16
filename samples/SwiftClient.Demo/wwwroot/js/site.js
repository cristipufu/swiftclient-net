

var ctrl = function () {
    this.initFileUpload();
    this.initTree();
    this.initVideo();
};

ctrl.prototype.initTree = function () {
    $('#tree').append(this.buildTree(this.getTreeData(), 0));
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

ctrl.prototype.buildTree = function (nodes, depth) {
    var $list = $('<ul class="list-group"></ul>');
    depth++;
    for (var i = 0; i < nodes.length; i++) {
        var $li = $('<li class="list-group-item"></li>');
        $li.append('<span>' + nodes[i].text + '</span>');
        if (nodes[i].nodes && nodes[i].nodes.length > 0) {
            var newDepth = depth;
            var $node = this.buildTree(nodes[i].nodes, newDepth);
            if (depth < 2){
                $li.prepend($('<span class="glyphicon glyphicon-minus pull-right tree-toogle js-treeToggle"></span>'))
            } else {
                $node.hide();
                $li.prepend($('<span class="glyphicon glyphicon-plus pull-right tree-toogle js-treeToggle"></span>'))
            }
            $li.append($node);
            $li.prepend('<span class="glyphicon glyphicon-folder-open"></span>');
        } else if (depth > 2) { // leaf
            $li.prepend('<span class="glyphicon glyphicon-file"></span>');
            $li.append('<div class="btn-group btn-group-sm pull-right" role="group"><a href="home/downloadfile?objectId=' + nodes[i].objectId + '&containerId=' + nodes[i].containerId + '" class="btn btn-primary js-downloadBtn"><span class="glyphicon glyphicon-download"></span> Download</a></div><div class="clearfix"></div>');
        } else {
            $li.prepend('<span class="glyphicon glyphicon-folder-open"></span>');
        }
        $list.append($li);
    }

    return $list;
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
