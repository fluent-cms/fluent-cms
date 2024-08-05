$(document).ready(function() {
    $('#saveView').on('click', function () {
        submit(save);
    });
    $('#viewAPI').on('click', function () {
        const val = editor.getValue();
        window.location.href = `/api/views/${val.name}`;
    });
});
    
    