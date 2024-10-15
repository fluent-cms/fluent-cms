$(document).ready(function() {
    $('#navbar-container').load('nav-bar.html',()=>{
        $('#nav-item-exit').on('click', function(e) {
            e.preventDefault();
            logout().then(()=>window.location.href='/');
        });
    });
});