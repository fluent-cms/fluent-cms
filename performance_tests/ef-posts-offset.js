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
let baseUrl = 'http://localhost:5184/posts_offset'
export default function () {
    for (let i = 10000; i < 10011; i++) {
        let subsequentRes = http.get(baseUrl + `?page=${i}`);
        check(subsequentRes, {
            'subsequent request status is 200': (r) => r.status === 200,
        });
    }
}
/*
8/7
running (3m04.0s), 0000/1000 VUs, 48344 complete and 0 interrupted iterations
default ✓ [======================================] 0000/1000 VUs  3m2s

     ✓ initial request status is 200
     ✓ subsequent request status is 200
     ✓ response contains items

     checks.........................: 100.00% ✓ 1015224     ✗ 0
     data_received..................: 4.0 GB  22 MB/s
     data_sent......................: 71 MB   385 kB/s
     http_req_blocked...............: avg=3.63µs   min=0s     med=1µs      max=89.03ms  p(90)=2µs      p(95)=3µs
     http_req_connecting............: avg=1.84µs   min=0s     med=0s       max=89ms     p(90)=0s       p(95)=0s
     http_req_duration..............: avg=204.63ms min=2.06ms med=207.03ms max=652.39ms p(90)=323.09ms p(95)=349.17ms
       { expected_response:true }...: avg=204.63ms min=2.06ms med=207.03ms max=652.39ms p(90)=323.09ms p(95)=349.17ms
     http_req_failed................: 0.00%   ✓ 0           ✗ 531784
     http_req_receiving.............: avg=431.81µs min=7µs    med=23µs     max=148.43ms p(90)=95µs     p(95)=248µs
     http_req_sending...............: avg=22.31µs  min=1µs    med=4µs      max=165.45ms p(90)=12µs     p(95)=19µs
     http_req_tls_handshaking.......: avg=0s       min=0s     med=0s       max=0s       p(90)=0s       p(95)=0s
     http_req_waiting...............: avg=204.17ms min=2.03ms med=206.59ms max=652.28ms p(90)=322.54ms p(95)=348.72ms
     http_reqs......................: 531784  2890.509673/s
     iteration_duration.............: avg=2.26s    min=31.7ms med=2.3s     max=4.37s    p(90)=3.52s    p(95)=3.77s
     iterations.....................: 48344   262.773607/s
     vus............................: 8       min=1         max=999
     vus_max........................: 1000    min=1000      max=1000
 */
