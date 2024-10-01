$(document).ready(function() {
    $('#saveQuery').on('click', function () {
        submit(save);
    });
    $('#visitAPI').on('click', function () {
        const val = editor.getValue();
        window.open(`/api/queries/${val.name}`,"_blank");
    });
}); 