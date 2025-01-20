//ulimit -n 10000
//ASPNETCORE_ENVIRONMENT=Production dotnet run
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
let baseUrl = 'http://localhost:5184/posts_join'
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
Date 8/9
running (3m03.6s), 0000/1000 VUs, 58835 complete and 0 interrupted iterations
default ✓ [======================================] 0000/1000 VUs  3m2s

     ✓ initial request status is 200
     ✓ subsequent request status is 200
     ✓ response contains items

checks.........................: 100.00% ✓ 1235535     ✗ 0
data_received..................: 4.5 GB  25 MB/s
data_sent......................: 89 MB   484 kB/s
http_req_blocked...............: avg=2.57µs   min=0s     med=1µs      max=68.86ms  p(90)=2µs      p(95)=3µs
http_req_connecting............: avg=1.11µs   min=0s     med=0s       max=68.81ms  p(90)=0s       p(95)=0s
http_req_duration..............: avg=167.68ms min=1.58ms med=175.3ms  max=650.17ms p(90)=255.68ms p(95)=269.38ms
{ expected_response:true }...: avg=167.68ms min=1.58ms med=175.3ms  max=650.17ms p(90)=255.68ms p(95)=269.38ms
http_req_failed................: 0.00%   ✓ 0           ✗ 647185
http_req_receiving.............: avg=114.89µs min=6µs    med=20µs     max=127.98ms p(90)=87µs     p(95)=195µs
http_req_sending...............: avg=10.93µs  min=1µs    med=4µs      max=96.1ms   p(90)=10µs     p(95)=15µs
http_req_tls_handshaking.......: avg=0s       min=0s     med=0s       max=0s       p(90)=0s       p(95)=0s
http_req_waiting...............: avg=167.56ms min=1.54ms med=175.17ms max=650.03ms p(90)=255.59ms p(95)=269.25ms
http_reqs......................: 647185  3524.678065/s
iteration_duration.............: avg=1.85s    min=25.5ms med=1.94s    max=3.1s     p(90)=2.78s    p(95)=2.87s
iterations.....................: 58835   320.425279/s
vus............................: 624     min=1         max=999
vus_max........................: 1000    min=1000      max=1000
 */