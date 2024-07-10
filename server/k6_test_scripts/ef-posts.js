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
running (3m04.4s), 0000/1000 VUs, 42687 complete and 0 interrupted iterations
default ✓ [======================================] 0000/1000 VUs  3m2s

     ✓ initial request status is 200
     ✓ subsequent request status is 200
     ✓ response contains items

     checks.........................: 100.00% ✓ 896427      ✗ 0
     data_received..................: 3.5 GB  19 MB/s
     data_sent......................: 63 MB   339 kB/s
     http_req_blocked...............: avg=1.99µs   min=0s      med=1µs      max=18.62ms  p(90)=2µs      p(95)=2µs
     http_req_connecting............: avg=871ns    min=0s      med=0s       max=18.45ms  p(90)=0s       p(95)=0s
     http_req_duration..............: avg=232.81ms min=3.13ms  med=227.4ms  max=801.73ms p(90)=361.15ms p(95)=397.25ms
       { expected_response:true }...: avg=232.81ms min=3.13ms  med=227.4ms  max=801.73ms p(90)=361.15ms p(95)=397.25ms
     http_req_failed................: 0.00%   ✓ 0           ✗ 469557
     http_req_receiving.............: avg=549.19µs min=8µs     med=22µs     max=138.32ms p(90)=99µs     p(95)=249µs
     http_req_sending...............: avg=6.49µs   min=1µs     med=4µs      max=17.8ms   p(90)=8µs      p(95)=11µs
     http_req_tls_handshaking.......: avg=0s       min=0s      med=0s       max=0s       p(90)=0s       p(95)=0s
     http_req_waiting...............: avg=232.25ms min=3.08ms  med=226.95ms max=801.03ms p(90)=360.21ms p(95)=396.07ms
     http_reqs......................: 469557  2546.973785/s
     iteration_duration.............: avg=2.56s    min=46.16ms med=2.59s    max=4.59s    p(90)=3.75s    p(95)=3.91s
     iterations.....................: 42687   231.543071/s
     vus............................: 416     min=1         max=999
     vus_max........................: 1000    min=1000      max=1000
 */