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
/*
7/14
running (3m04.0s), 0000/1000 VUs, 44396 complete and 0 interrupted iterations
default ✓ [======================================] 0000/1000 VUs  3m2s

     ✓ initial request status is 200
     ✓ subsequent request status is 200
     ✓ response contains items

     checks.........................: 100.00% ✓ 932316      ✗ 0
     data_received..................: 3.6 GB  20 MB/s
     data_sent......................: 65 MB   353 kB/s
     http_req_blocked...............: avg=1.65µs   min=0s      med=1µs      max=7.77ms   p(90)=1µs      p(95)=2µs
     http_req_connecting............: avg=654ns    min=0s      med=0s       max=2.47ms   p(90)=0s       p(95)=0s
     http_req_duration..............: avg=223.19ms min=2.53ms  med=220.84ms max=771.67ms p(90)=342.61ms p(95)=373.5ms
       { expected_response:true }...: avg=223.19ms min=2.53ms  med=220.84ms max=771.67ms p(90)=342.61ms p(95)=373.5ms
     http_req_failed................: 0.00%   ✓ 0           ✗ 488356
     http_req_receiving.............: avg=507.52µs min=8µs     med=21µs     max=125ms    p(90)=87µs     p(95)=179µs
     http_req_sending...............: avg=5.65µs   min=1µs     med=4µs      max=8.41ms   p(90)=8µs      p(95)=10µs
     http_req_tls_handshaking.......: avg=0s       min=0s      med=0s       max=0s       p(90)=0s       p(95)=0s
     http_req_waiting...............: avg=222.68ms min=2.47ms  med=220.47ms max=771.5ms  p(90)=341.75ms p(95)=372.02ms
     http_reqs......................: 488356  2653.795461/s
     iteration_duration.............: avg=2.46s    min=46.38ms med=2.47s    max=4.48s    p(90)=3.57s    p(95)=3.7s
     iterations.....................: 44396   241.254133/s
     vus............................: 200     min=1         max=999
     vus_max........................: 1000    min=1000      max=1000
 */
