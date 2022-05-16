$(document).ready(function () {

    $("#btnRecord").click(function () {
        $("body").prepend('<div id="preloader" style="background-color:green;">Recording...</div>');
        var speechModel = new CreateSpeechModelForSave();
        $.ajax({
            url: '/speech/SaveSpeech',
            contentType: 'application/json',
            type: 'Post',
            data: JSON.stringify(speechModel),
            success: function (data) {
                $("#preloader").remove();
            }
        });
    });

    $("#btnSavePlay").click(function () {
        var speechModel = new CreateSpeechModel();
        $.ajax({
            url: '/speech/SaveType/' + $('input[name="optoutnameradio"]:checked').val() + '/' + $("#adEntId").text(),
            contentType: 'application/json',
            type: 'GET',
            success: function (data) {
                if ($('input[name="optoutnameradio"]:checked').val() === 'Standard') {
                    $.ajax({
                        url: '/speech/TextToSpeech',
                        contentType: 'application/json',
                        type: 'Post',
                        data: JSON.stringify(speechModel),
                        success: function (data) {

                        }
                    })
                }
                else {
                    $.ajax({
                        url: '/speech/PlayAudio/' + $("#adEntId").text(),
                        contentType: 'application/json',
                        type: 'GET',
                        success: function (data) {

                        }
                    })
                }
            }
        });
    });
});

function CreateSpeechModelForSave() {
    var speechModel = this;
    speechModel.EmployeeAdEntId = $("#adEntId").val();
    return speechModel;
};

function CreateSpeechModel() {
    var speechModel = this;
    speechModel.EmployeeAdEntId = $("#adEntId").text();
    speechModel.Name = $("#preferredName").text() != null && $("#preferredName").text() != undefined && $("#preferredName").text() != ''
        ? $("#preferredName").text().trim() : $("#empName").text();
    speechModel.Region = $("#country").text();
    return speechModel;
}