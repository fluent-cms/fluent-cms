$(document).ready(function() {
    $('#saveQuery').on('click', function () {
        submit(save);
    });
    $('#visitAPI').on('click', function () {
        const val = editor.getValue();
        window.location.href = `/api/queries/${val.name}`;
    });
});
    
    