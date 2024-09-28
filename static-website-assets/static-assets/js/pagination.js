$(document).ready(function() {
    var loadingDict = new Map();
    initIntersectionObserver();
    
    function loadMore(token, render) {
        if (!token || loadingDict[token]){
            return
        }
        loadingDict[token] = true;
        $.ajax({
            url: '/pages', 
            type: 'GET',
            data: {
                token,
            },
            success: function(response) {
                render(response);
                loadingDict[token] = false;
           },
            error: function() {
                console.log('Error loading more.');
                loadingDict[token] = false;
            }
        });
    }
    
    function initIntersectionObserver() {
        let observer = new IntersectionObserver(function (entries) {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    loadMore(  entry.target.attributes['last'].value, response=>{
                        const template = document.createElement('template');
                        template.innerHTML = response.trim();
                        entry.target.parentElement.appendChild(content);
                        entry.target.remove();
                        initIntersectionObserver();
                    });
                }
            });
        });

        // Observe the hidden element
        let ele = document.querySelector(".load-more-trigger");
        if (ele) observer.observe(ele);
    }

    $('[data-command="next"]').click(function(e) {
        event.preventDefault(); // Prevent default anchor behavior
        let target = e.target;
        let parent = target.parentElement.parentElement;
        let list = parent.querySelector('[data-source-type="multiple-records"]');
        loadMore(list.attributes['last'].value, response =>{
            list.outerHTML = response;
        })
    });
    
    $('[data-command="previous"]').click(function(e) {
        event.preventDefault();
        let target = e.target;
        let parent = target.parentElement.parentElement;
        let list = parent.querySelector('[data-source-type="multiple-records"]');
        loadMore(list.attributes['first'].value, response =>{
            list.outerHTML = response;
        })
    });
});
