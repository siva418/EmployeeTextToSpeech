$(document).ready(function () {

    $("#btnRecord").click(function () {
        $("body").prepend('<div id="preloader" style="background-color:green;">Recording...</div>');
        var speechModel = new CreateSpeechModel();
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
});

function CreateSpeechModel() {
    var speechModel = this;
    speechModel.EmployeeAdEntId = $("#adEntId").val();
    return speechModel;
};