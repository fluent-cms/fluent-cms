import http from 'k6/http';
import { check, sleep } from 'k6';
import { Counter } from 'k6/metrics';

export let errorCount = new Counter('errors');

export let options = {
    stages: [
        { duration: '1s', target: 1 },
        { duration: '1s', target: 100 },
        { duration: '1m', target: 500 },
        { duration: '2m', target: 1000 },
    ],
};
let baseUrl = 'http://localhost:9080/posts'
export default function () {
    // Initial request to get the `last` parameter
    let initialRes = http.get(baseUrl);

    check(initialRes, {
        'initial request status is 200': (r) => r.status === 200,
    });

    if (initialRes.status !== 200) {
        errorCount.add(1);
        return;
    }

    let initialResponse = JSON.parse(initialRes.body);
    let lastParam = initialResponse.last;

    // Loop to make subsequent requests 10 times
    for (let i = 0; i < 10; i++) {
        let subsequentRes = http.get(baseUrl + `?last=${lastParam}`);

        check(subsequentRes, {
            'subsequent request status is 200': (r) => r.status === 200,
            'response contains items': (r) => JSON.parse(r.body).items.length > 0,
        });

        if (subsequentRes.status !== 200) {
            errorCount.add(1);
        } else {
            let subsequentResponse = JSON.parse(subsequentRes.body);
            lastParam = subsequentResponse.last; // Update lastParam for the next request
        }
    }
}
//ulimit -n 1048576
//create index posts_deleted_published_at_index on posts (deleted asc, published_at desc);
//create index post_authors_deleted_post_id_index on public.post_authors (deleted asc, post_id desc);

/*
 scenarios: (100.00%) 1 scenario, 1000 max VUs, 3m32s max duration (incl. graceful stop):
           * default: Up to 1000 looping VUs for 3m2s over 4 stages (gracefulRampDown: 30s, gracefulStop: 30s)


running (3m32.0s), 0000/1000 VUs, 824 complete and 886 interrupted iterations
default ✓ [======================================] 0886/1000 VUs  3m2s

     ✓ initial request status is 200
     ✓ subsequent request status is 200
     ✓ response contains items

     checks.........................: 100.00% ✓ 26788     ✗ 0
     data_received..................: 107 MB  506 kB/s
     data_sent......................: 2.0 MB  9.4 kB/s
     http_req_blocked...............: avg=37.44µs  min=0s       med=4µs   max=10.17ms p(90)=9µs     p(95)=342µs
     http_req_connecting............: avg=27.34µs  min=0s       med=0s    max=10.11ms p(90)=0s      p(95)=285µs
     http_req_duration..............: avg=9.14s    min=114.21ms med=9.59s max=15.32s  p(90)=14.07s  p(95)=14.46s
       { expected_response:true }...: avg=9.14s    min=114.21ms med=9.59s max=15.32s  p(90)=14.07s  p(95)=14.46s
     http_req_failed................: 0.00%   ✓ 0         ✗ 14249
     http_req_receiving.............: avg=162.31µs min=15µs     med=86µs  max=35.78ms p(90)=301.2µs p(95)=592µs
     http_req_sending...............: avg=26.32µs  min=4µs      med=18µs  max=8.34ms  p(90)=40µs    p(95)=61µs
     http_req_tls_handshaking.......: avg=0s       min=0s       med=0s    max=0s      p(90)=0s      p(95)=0s
     http_req_waiting...............: avg=9.13s    min=114.1ms  med=9.59s max=15.32s  p(90)=14.07s  p(95)=14.46s
     http_reqs......................: 14249   67.208765/s
     iteration_duration.............: avg=1m17s    min=19.27s   med=1m18s max=2m9s    p(90)=1m59s   p(95)=2m5s
     iterations.....................: 824     3.88659/s
     vus............................: 886     min=1       max=999
     vus_max........................: 1000    min=1000    max=1000
     
scenarios: (100.00%) 1 scenario, 1000 max VUs, 3m32s max duration (incl. graceful stop):
           * default: Up to 1000 looping VUs for 3m2s over 4 stages (gracefulRampDown: 30s, gracefulStop: 30s)

running (3m32.0s), 0000/1000 VUs, 789 complete and 896 interrupted iterations
default ✓ [======================================] 0895/1000 VUs  3m2s
     ✓ initial request status is 200
     ✓ subsequent request status is 200
     ✓ response contains items
     checks.........................: 100.00% ✓ 25653     ✗ 0
     data_received..................: 103 MB  486 kB/s
     data_sent......................: 1.9 MB  9.1 kB/s
     http_req_blocked...............: avg=44.19µs  min=0s       med=4µs   max=17.51ms p(90)=11µs    p(95)=386µs
     http_req_connecting............: avg=32.08µs  min=0s       med=0s    max=17.38ms p(90)=0s      p(95)=307µs
     http_req_duration..............: avg=9.51s    min=114.23ms med=9.7s  max=16.37s  p(90)=14.91s  p(95)=15.28s
       { expected_response:true }...: avg=9.51s    min=114.23ms med=9.7s  max=16.37s  p(90)=14.91s  p(95)=15.28s
     http_req_failed................: 0.00%   ✓ 0         ✗ 13669
     http_req_receiving.............: avg=211.41µs min=12µs     med=106µs max=9.02ms  p(90)=398.2µs p(95)=677µs
     http_req_sending...............: avg=28.76µs  min=3µs      med=20µs  max=6.34ms  p(90)=47µs    p(95)=69.59µs
     http_req_tls_handshaking.......: avg=0s       min=0s       med=0s    max=0s      p(90)=0s      p(95)=0s
     http_req_waiting...............: avg=9.51s    min=114.09ms med=9.7s  max=16.37s  p(90)=14.91s  p(95)=15.27s
     http_reqs......................: 13669   64.469955/s
     iteration_duration.............: avg=1m20s    min=16.1s    med=1m23s max=2m12s   p(90)=2m1s    p(95)=2m7s
     iterations.....................: 789     3.721325/s
     vus............................: 896     min=1       max=999
     vus_max........................: 1000    min=1000    max=1000
*
scenarios: (100.00%) 1 scenario, 1000 max VUs, 3m32s max duration (incl. graceful stop):
* default: Up to 1000 looping VUs for 3m2s over 4 stages (gracefulRampDown: 30s, gracefulStop: 30s)

running (3m32.0s), 0000/1000 VUs, 780 complete and 895 interrupted iterations
default ✓ [======================================] 0894/1000 VUs  3m2s

     ✓ initial request status is 200
     ✓ subsequent request status is 200
     ✓ response contains items

checks.........................: 100.00% ✓ 25547     ✗ 0
data_received..................: 103 MB  484 kB/s
data_sent......................: 1.9 MB  9.0 kB/s
http_req_blocked...............: avg=45.52µs  min=0s      med=4µs   max=5.88ms   p(90)=10µs   p(95)=396µs
http_req_connecting............: avg=32.95µs  min=0s      med=0s    max=3.94ms   p(90)=0s     p(95)=314µs
http_req_duration..............: avg=9.55s    min=76.53ms med=9.67s max=16.26s   p(90)=14.76s p(95)=15.17s
{ expected_response:true }...: avg=9.55s    min=76.53ms med=9.67s max=16.26s   p(90)=14.76s p(95)=15.17s
http_req_failed................: 0.00%   ✓ 0         ✗ 13611
http_req_receiving.............: avg=268.93µs min=14µs    med=109µs max=153.46ms p(90)=512µs  p(95)=750µs
http_req_sending...............: avg=30.44µs  min=3µs     med=20µs  max=10.03ms  p(90)=46µs   p(95)=71µs
http_req_tls_handshaking.......: avg=0s       min=0s      med=0s    max=0s       p(90)=0s     p(95)=0s
http_req_waiting...............: avg=9.55s    min=76.43ms med=9.67s max=16.26s   p(90)=14.76s p(95)=15.17s
http_reqs......................: 13611   64.197057/s
iteration_duration.............: avg=1m20s    min=1.02s   med=1m22s max=2m14s    p(90)=2m2s   p(95)=2m8s
iterations.....................: 780     3.678914/s
vus............................: 895     min=1       max=999
vus_max........................: 1000    min=1000    max=1000
 */