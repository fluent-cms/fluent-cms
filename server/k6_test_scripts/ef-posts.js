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
scenarios: (100.00%) 1 scenario, 1000 max VUs, 3m32s max duration (incl. graceful stop):
           * default: Up to 1000 looping VUs for 3m2s over 4 stages (gracefulRampDown: 30s, gracefulStop: 30s)


running (3m04.3s), 0000/1000 VUs, 41707 complete and 0 interrupted iterations
default ✓ [======================================] 0000/1000 VUs  3m2s

     ✓ initial request status is 200
     ✓ subsequent request status is 200
     ✓ response contains items

     checks.........................: 100.00% ✓ 875847      ✗ 0
     data_received..................: 3.7 GB  20 MB/s
     data_sent......................: 61 MB   332 kB/s
     http_req_blocked...............: avg=1.95µs   min=0s      med=1µs      max=9.3ms    p(90)=2µs      p(95)=2µs
     http_req_connecting............: avg=797ns    min=0s      med=0s       max=4.99ms   p(90)=0s       p(95)=0s
     http_req_duration..............: avg=237.98ms min=2.7ms   med=236.56ms max=715.47ms p(90)=360.39ms p(95)=389.47ms
       { expected_response:true }...: avg=237.98ms min=2.7ms   med=236.56ms max=715.47ms p(90)=360.39ms p(95)=389.47ms
     http_req_failed................: 0.00%   ✓ 0           ✗ 458777
     http_req_receiving.............: avg=998.16µs min=9µs     med=24µs     max=152.01ms p(90)=131µs    p(95)=2.22ms
     http_req_sending...............: avg=6.62µs   min=1µs     med=4µs      max=15.47ms  p(90)=9µs      p(95)=12µs
     http_req_tls_handshaking.......: avg=0s       min=0s      med=0s       max=0s       p(90)=0s       p(95)=0s
     http_req_waiting...............: avg=236.97ms min=2.65ms  med=235.72ms max=715.19ms p(90)=358.49ms p(95)=386.57ms
     http_reqs......................: 458777  2489.647353/s
     iteration_duration.............: avg=2.62s    min=45.23ms med=2.69s    max=4.52s    p(90)=3.79s    p(95)=3.93s
     iterations.....................: 41707   226.331578/s
     vus............................: 394     min=1         max=999
     vus_max........................: 1000    min=1000      max=1000

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
