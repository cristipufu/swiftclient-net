$(function () {

    var chunkIndex = 0;

    $('#fileupload').fileupload({
        dataType: 'json',
        maxChunkSize: 1000000,
        add: function (e, data) {
            var ext = (data.files != null && data.files[0] != null && data.files[0].name != null) ? data.files[0].name.substr(data.files[0].name.lastIndexOf('.') + 1) : '';
            if (ext == 'mp4' || ext == 'MP4') data.submit();;
            return false;
        },
        formData: function () {
            chunkIndex++;
            return [{
                name: 'segment',
                value: chunkIndex
            }]
        },
        done: function (e, data) {

            if (data.result.Success) {

                $.ajax({
                    url: 'home/uploaddone',
                    dataType: 'json',
                    success: function (response) {
                        console.log(response);
                    }
                })

            }
        }
    });

});
