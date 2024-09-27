$(document).ready(function() {
    var loading = false;

    function loadMoreCourses(target) {
        let last = target.attributes['last'].value;
        if (!last){
            return
        }
        
        if (loading) return;
        loading = true;
        $.ajax({
            url: '/pages', 
            type: 'GET',
            data: {
                token: last,
            },
            success: function(response) {
                const template = document.createElement('template');
                template.innerHTML = response.trim();
                target.parentElement.appendChild(template.content);
                target.remove();
                loading = false;
            },
            error: function() {
                console.log('Error loading more.');
                loading = false;
            }
        });
    }
    
    let observer = new IntersectionObserver(function(entries) {
        entries.forEach(entry => {
            if (entry.isIntersecting && !loading) {
                loadMoreCourses(entry.target);  // Load more courses when the hidden element is in view
            }
        });
    });

    // Observe the hidden element
    observer.observe(document.querySelector(".load-more-trigger"));
});
