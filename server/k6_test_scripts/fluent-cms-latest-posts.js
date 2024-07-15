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
Date: 7/14
scenarios: (100.00%) 1 scenario, 1000 max VUs, 3m32s max duration (incl. graceful stop):
           * default: Up to 1000 looping VUs for 3m2s over 4 stages (gracefulRampDown: 30s, gracefulStop: 30s)


running (3m03.7s), 0000/1000 VUs, 52696 complete and 0 interrupted iterations
default ✓ [======================================] 0000/1000 VUs  3m2s

     ✓ initial request status is 200
     ✓ subsequent request status is 200
     ✓ response contains items

     checks.........................: 100.00% ✓ 1106616     ✗ 0
     data_received..................: 4.5 GB  25 MB/s
     data_sent......................: 94 MB   511 kB/s
     http_req_blocked...............: avg=1.63µs   min=0s      med=1µs      max=8.66ms   p(90)=2µs      p(95)=2µs
     http_req_connecting............: avg=580ns    min=0s      med=0s       max=4.88ms   p(90)=0s       p(95)=0s
     http_req_duration..............: avg=187.62ms min=2.51ms  med=192.38ms max=746.3ms  p(90)=282.55ms p(95)=298.57ms
       { expected_response:true }...: avg=187.62ms min=2.51ms  med=192.38ms max=746.3ms  p(90)=282.55ms p(95)=298.57ms
     http_req_failed................: 0.00%   ✓ 0           ✗ 579656
     http_req_receiving.............: avg=159.29µs min=8µs     med=23µs     max=82.9ms   p(90)=101µs    p(95)=218µs
     http_req_sending...............: avg=6.04µs   min=1µs     med=4µs      max=8.12ms   p(90)=8µs      p(95)=10µs
     http_req_tls_handshaking.......: avg=0s       min=0s      med=0s       max=0s       p(90)=0s       p(95)=0s
     http_req_waiting...............: avg=187.46ms min=2.44ms  med=192.29ms max=745.51ms p(90)=282.21ms p(95)=298.15ms
     http_reqs......................: 579656  3155.969089/s
     iteration_duration.............: avg=2.06s    min=38.31ms med=2.11s    max=3.4s     p(90)=3.12s    p(95)=3.22s
     iterations.....................: 52696   286.906281/s
     vus............................: 642     min=1         max=999
     vus_max........................: 1000    min=1000      max=1000
*/

