//ASPNETCORE_ENVIRONMENT=Production DatabaseProvider=Postgres dotnet run

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

let baseUrl = 'http://localhost:5096/api/views/latest-posts'
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
/* Date 8/7
scenarios: (100.00%) 1 scenario, 1000 max VUs, 3m32s max duration (incl. graceful stop):
           * default: Up to 1000 looping VUs for 3m2s over 4 stages (gracefulRampDown: 30s, gracefulStop: 30s)


running (3m03.6s), 0000/1000 VUs, 59389 complete and 0 interrupted iterations
default ✓ [======================================] 0000/1000 VUs  3m2s

     ✓ initial request status is 200
     ✓ subsequent request status is 200
     ✓ response contains items

     checks.........................: 100.00% ✓ 1247169     ✗ 0
     data_received..................: 4.9 GB  27 MB/s
     data_sent......................: 106 MB  576 kB/s
     http_req_blocked...............: avg=11.91µs  min=0s      med=1µs      max=210.92ms p(90)=2µs      p(95)=3µs
     http_req_connecting............: avg=9.03µs   min=0s      med=0s       max=177.28ms p(90)=0s       p(95)=0s
     http_req_duration..............: avg=162.84ms min=1.29ms  med=174.66ms max=661.31ms p(90)=256.11ms p(95)=276.89ms
       { expected_response:true }...: avg=162.84ms min=1.29ms  med=174.66ms max=661.31ms p(90)=256.11ms p(95)=276.89ms
     http_req_failed................: 0.00%   ✓ 0           ✗ 653279
     http_req_receiving.............: avg=1.63ms   min=6µs     med=18µs     max=495.97ms p(90)=108µs    p(95)=658µs
     http_req_sending...............: avg=70.27µs  min=1µs     med=4µs      max=490.07ms p(90)=10µs     p(95)=18µs
     http_req_tls_handshaking.......: avg=0s       min=0s      med=0s       max=0s       p(90)=0s       p(95)=0s
     http_req_waiting...............: avg=161.13ms min=1.15ms  med=173.06ms max=374.25ms p(90)=252.88ms p(95)=273.51ms
     http_reqs......................: 653279  3557.419406/s
     iteration_duration.............: avg=1.83s    min=29.43ms med=2.01s    max=3.23s    p(90)=2.73s    p(95)=2.88s
     iterations.....................: 59389   323.401764/s
     vus............................: 649     min=1         max=999
     vus_max........................: 1000    min=1000      max=1000

Date: 7/14
scenarios: (100.00%) 1 scenario, 1000 max VUs, 3m32s max duration (incl. graceful stop):
           * default: Up to 1000 looping VUs for 3m2s over 4 stages (gracefulRampDown: 30s, gracefulStop: 30s)
 */