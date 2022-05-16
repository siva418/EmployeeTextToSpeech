$(document).ready(function () {
    $("#btnplay").click(function () {
        var speechModel = new CreateSpeechModel();
        $.ajax({
            url: '/speech/TextToSpeech',
            contentType: 'application/json',
            type: 'Post',
            data: JSON.stringify(speechModel),
            success: function (data) {

            }
        })
    })
});

function CreateSpeechModel() {
    var speechModel = this;
    speechModel.EmployeeAdEntId = "U819140";// $("ADEntId").val();
    speechModel.Name = "Riyaz Pasha";//$("").val();
    speechModel.Region = "India";// $("").val();
    return speechModel;
}