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

//let baseUrl = 'http://localhost:5000/api/views/latest-posts'
let baseUrl = 'http://localhost:8080/api/views/latest-posts'
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
/*
 scenarios: (100.00%) 1 scenario, 1000 max VUs, 3m32s max duration (incl. graceful stop):
           * default: Up to 1000 looping VUs for 3m2s over 4 stages (gracefulRampDown: 30s, gracefulStop: 30s)


running (3m08.1s), 0000/1000 VUs, 17517 complete and 0 interrupted iterations
default ✓ [======================================] 0000/1000 VUs  3m2s

     ✓ initial request status is 200
     ✓ subsequent request status is 200
     ✓ response contains items

     checks.........................: 100.00% ✓ 367857      ✗ 0
     data_received..................: 1.5 GB  7.9 MB/s
     data_sent......................: 31 MB   166 kB/s
     http_req_blocked...............: avg=3.3µs    min=0s       med=1µs      max=6.68ms p(90)=2µs      p(95)=3µs
     http_req_connecting............: avg=1.7µs    min=0s       med=0s       max=6.64ms p(90)=0s       p(95)=0s
     http_req_duration..............: avg=581.02ms min=6.5ms    med=575.23ms max=1.38s  p(90)=926.85ms p(95)=965.43ms
       { expected_response:true }...: avg=581.02ms min=6.5ms    med=575.23ms max=1.38s  p(90)=926.85ms p(95)=965.43ms
     http_req_failed................: 0.00%   ✓ 0           ✗ 192687
     http_req_receiving.............: avg=328.86µs min=9µs      med=40µs     max=89.6ms p(90)=352µs    p(95)=1.22ms
     http_req_sending...............: avg=8.88µs   min=2µs      med=8µs      max=3.44ms p(90)=13µs     p(95)=16µs
     http_req_tls_handshaking.......: avg=0s       min=0s       med=0s       max=0s     p(90)=0s       p(95)=0s
     http_req_waiting...............: avg=580.68ms min=6.4ms    med=574.91ms max=1.38s  p(90)=926.47ms p(95)=965.04ms
     http_reqs......................: 192687  1024.561066/s
     iteration_duration.............: avg=6.39s    min=167.68ms med=6.49s    max=11.17s p(90)=10.14s   p(95)=10.36s
     iterations.....................: 17517   93.141915/s
     vus............................: 181     min=1         max=999
     vus_max........................: 1000    min=1000      max=1000
     
scenarios: (100.00%) 1 scenario, 1000 max VUs, 3m32s max duration (incl. graceful stop):
           * default: Up to 1000 looping VUs for 3m2s over 4 stages (gracefulRampDown: 30s, gracefulStop: 30s)


running (3m09.0s), 0000/1000 VUs, 15041 complete and 0 interrupted iterations
default ✓ [======================================] 0000/1000 VUs  3m2s

     ✓ initial request status is 200
     ✓ subsequent request status is 200
     ✓ response contains items

     checks.........................: 100.00% ✓ 315861     ✗ 0     
     data_received..................: 1.3 GB  6.8 MB/s
     data_sent......................: 27 MB   142 kB/s
     http_req_blocked...............: avg=4.46µs   min=0s      med=1µs      max=44.57ms p(90)=2µs    p(95)=3µs   
     http_req_connecting............: avg=2.47µs   min=0s      med=0s       max=43.27ms p(90)=0s     p(95)=0s    
     http_req_duration..............: avg=680.88ms min=12.68ms med=712.98ms max=1.99s   p(90)=1.01s  p(95)=1.08s 
       { expected_response:true }...: avg=680.88ms min=12.68ms med=712.98ms max=1.99s   p(90)=1.01s  p(95)=1.08s 
     http_req_failed................: 0.00%   ✓ 0          ✗ 165451
     http_req_receiving.............: avg=342.11µs min=10µs    med=44µs     max=83.59ms p(90)=383µs  p(95)=1.27ms
     http_req_sending...............: avg=10.31µs  min=2µs     med=8µs      max=7.37ms  p(90)=15µs   p(95)=20µs  
     http_req_tls_handshaking.......: avg=0s       min=0s      med=0s       max=0s      p(90)=0s     p(95)=0s    
     http_req_waiting...............: avg=680.53ms min=12.52ms med=712.56ms max=1.99s   p(90)=1.01s  p(95)=1.08s 
     http_reqs......................: 165451  875.186501/s
     iteration_duration.............: avg=7.49s    min=1.56s   med=8.06s    max=12.82s  p(90)=10.84s p(95)=11.5s 
     iterations.....................: 15041   79.562409/s
     vus............................: 157     min=1        max=999 
     vus_max........................: 1000    min=1000     max=1000

scenarios: (100.00%) 1 scenario, 1000 max VUs, 3m31s max duration (incl. graceful stop):
           * default: Up to 1000 looping VUs for 3m1s over 3 stages (gracefulRampDown: 30s, gracefulStop: 30s)


running (3m08.5s), 0000/1000 VUs, 14277 complete and 0 interrupted iterations
default ✓ [======================================] 0000/1000 VUs  3m1s

     ✓ initial request status is 200
     ✓ subsequent request status is 200
     ✓ response contains items

     checks.........................: 100.00% ✓ 299817     ✗ 0     
     data_received..................: 1.2 GB  6.5 MB/s
     data_sent......................: 25 MB   135 kB/s
     http_req_blocked...............: avg=4.46µs   min=0s      med=1µs      max=5.63ms  p(90)=3µs    p(95)=4µs   
     http_req_connecting............: avg=2.36µs   min=0s      med=0s       max=3.37ms  p(90)=0s     p(95)=0s    
     http_req_duration..............: avg=717.75ms min=59.07ms med=691.65ms max=2.62s   p(90)=1.11s  p(95)=1.23s 
       { expected_response:true }...: avg=717.75ms min=59.07ms med=691.65ms max=2.62s   p(90)=1.11s  p(95)=1.23s 
     http_req_failed................: 0.00%   ✓ 0          ✗ 157047
     http_req_receiving.............: avg=383.74µs min=10µs    med=48µs     max=61.23ms p(90)=459µs  p(95)=1.52ms
     http_req_sending...............: avg=11.14µs  min=2µs     med=8µs      max=19.67ms p(90)=16µs   p(95)=20µs  
     http_req_tls_handshaking.......: avg=0s       min=0s      med=0s       max=0s      p(90)=0s     p(95)=0s    
     http_req_waiting...............: avg=717.35ms min=59.02ms med=691.01ms max=2.62s   p(90)=1.11s  p(95)=1.23s 
     http_reqs......................: 157047  833.179865/s
     iteration_duration.............: avg=7.9s     min=2.28s   med=7.69s    max=13.71s  p(90)=12.14s p(95)=12.73s
     iterations.....................: 14277   75.743624/s
     vus............................: 265     min=91       max=999 
     vus_max........................: 1000    min=1000     max=1000
 */
