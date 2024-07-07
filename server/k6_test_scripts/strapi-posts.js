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

let baseUrl= 'http://localhost:1337/api/posts?populate=*&sort[0]=published_time:desc&pagination[pageSize]=10'

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
    let lastParam = initialResponse.data.pop().attributes.published_time;

    // Loop to make subsequent requests 10 times
    for (let i = 0; i < 10; i++) {
        var url = `${baseUrl}&filters[published_time][$lt]=${lastParam}`
        let subsequentRes = http.get(url);

        check(subsequentRes, {
            'subsequent request status is 200': (r) => r.status === 200,
            'response contains items': (r) => JSON.parse(r.body).data.length > 0,
        });

        if (subsequentRes.status !== 200) {
            errorCount.add(1);
        } else {
            let subsequentResponse = JSON.parse(subsequentRes.body);
            lastParam = subsequentResponse.data.pop().attributes.published_time; // Update lastParam for the next request
        }
    }
}
/*
scenarios: (100.00%) 1 scenario, 1000 max VUs, 3m32s max duration (incl. graceful stop):
           * default: Up to 1000 looping VUs for 3m2s over 4 stages (gracefulRampDown: 30s, gracefulStop: 30s)


running (3m32.1s), 0000/1000 VUs, 0 complete and 999 interrupted iterations
default ✓ [======================================] 0999/1000 VUs  3m2s
WARN[0212] No script iterations finished, consider making the test duration longer 

     ✓ initial request status is 200
     ✓ subsequent request status is 200
     ✓ response contains items

     checks.........................: 100.00% ✓ 6702      ✗ 0     
     data_received..................: 40 MB   190 kB/s
     data_sent......................: 936 kB  4.4 kB/s
     http_req_blocked...............: avg=131.61µs min=1µs      med=5µs    max=9.66ms  p(90)=500.8µs p(95)=610µs   
     http_req_connecting............: avg=102.26µs min=0s       med=0s     max=9.35ms  p(90)=382µs   p(95)=489.69µs
     http_req_duration..............: avg=29.1s    min=209.11ms med=29.82s max=51.82s  p(90)=46.99s  p(95)=49.32s  
       { expected_response:true }...: avg=29.1s    min=209.11ms med=29.82s max=51.82s  p(90)=46.99s  p(95)=49.32s  
     http_req_failed................: 0.00%   ✓ 0         ✗ 3807  
     http_req_receiving.............: avg=188.84µs min=39µs     med=155µs  max=14.42ms p(90)=270µs   p(95)=327.69µs
     http_req_sending...............: avg=45.47µs  min=7µs      med=28µs   max=3.32ms  p(90)=88µs    p(95)=115µs   
     http_req_tls_handshaking.......: avg=0s       min=0s       med=0s     max=0s      p(90)=0s      p(95)=0s      
     http_req_waiting...............: avg=29.1s    min=208.94ms med=29.82s max=51.82s  p(90)=46.99s  p(95)=49.32s  
     http_reqs......................: 3807    17.952644/s
     vus............................: 999     min=1       max=999 
     vus_max........................: 1000    min=1000    max=1000

scenarios: (100.00%) 1 scenario, 1000 max VUs, 3m31s max duration (incl. graceful stop):
* default: Up to 1000 looping VUs for 3m1s over 3 stages (gracefulRampDown: 30s, gracefulStop: 30s)


running (3m31.0s), 0000/1000 VUs, 7 complete and 992 interrupted iterations
default ✓ [======================================] 0432/1000 VUs  3m1s

     ✓ initial request status is 200
     ✓ subsequent request status is 200
     ✓ response contains items

checks.........................: 100.00% ✓ 6656      ✗ 0
data_received..................: 40 MB   189 kB/s
data_sent......................: 929 kB  4.4 kB/s
http_req_blocked...............: avg=128.27µs min=1µs      med=5µs    max=7.35ms p(90)=481µs  p(95)=588µs
http_req_connecting............: avg=97.32µs  min=0s       med=0s     max=4.32ms p(90)=374µs  p(95)=461µs
http_req_duration..............: avg=29.17s   min=276.57ms med=30.43s max=53.27s p(90)=48.51s p(95)=50.83s
{ expected_response:true }...: avg=29.17s   min=276.57ms med=30.43s max=53.27s p(90)=48.51s p(95)=50.83s
http_req_failed................: 0.00%   ✓ 0         ✗ 3781
http_req_receiving.............: avg=192.7µs  min=38µs     med=157µs  max=5.56ms p(90)=283µs  p(95)=349µs
http_req_sending...............: avg=45.85µs  min=7µs      med=30µs   max=1.35ms p(90)=85µs   p(95)=112µs
http_req_tls_handshaking.......: avg=0s       min=0s       med=0s     max=0s     p(90)=0s     p(95)=0s
http_req_waiting...............: avg=29.17s   min=276.36ms med=30.43s max=53.27s p(90)=48.51s p(95)=50.83s
http_reqs......................: 3781    17.917048/s
iteration_duration.............: avg=3m28s    min=3m23s    med=3m29s  max=3m30s  p(90)=3m30s  p(95)=3m30s
iterations.....................: 7       0.033171/s
vus............................: 992     min=92      max=999
vus_max........................: 1000    min=1000    max=1000
 */
