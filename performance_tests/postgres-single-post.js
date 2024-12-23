import http from 'k6/http';
import { sleep, check } from 'k6';

export const options = {
    stages: [
        { duration: '30s', target: 50 }, // Ramp-up to 50 users over 30 seconds
        { duration: '1m', target: 1000 },  // Stay at 50 users for 1 minute
        { duration: '30s', target: 0 },  // Ramp-down to 0 users over 30 seconds
    ],
};

export default function () {
    const id = Math.floor(Math.random() * 1000000) + 1; // Generate random id between 1 and 1,000,000
    /*query RDBMS and Merge*/
    const url = `http://localhost:5091/api/queries/post_sync/?id=${id}`;
    
    /*query document Db*/
    //const url = `http://localhost:5091/api/queries/post/?id=${id}`;

    const res = http.get(url);

    check(res, {
        'is status 200': (r) => r.status === 200,
        'response time is < 200ms': (r) => r.timings.duration < 200,
    });
}
/*
mongo:

     checks.........................: 94.26% 1404391 out of 1489772
     data_received..................: 449 MB 3.7 MB/s
     data_sent......................: 80 MB  663 kB/s
     http_req_blocked...............: avg=32.83µs  min=0s       med=1µs     max=661.14ms p(90)=4µs      p(95)=6µs     
     http_req_connecting............: avg=20.97µs  min=0s       med=0s      max=566.66ms p(90)=0s       p(95)=0s      
     http_req_duration..............: avg=51.04ms  min=787µs    med=24.19ms max=3.23s    p(90)=126.45ms p(95)=211.99ms
       { expected_response:true }...: avg=50.04ms  min=787µs    med=20.22ms max=3.23s    p(90)=131.9ms  p(95)=218.66ms
     http_req_failed................: 5.88%  43812 out of 744886
     http_req_receiving.............: avg=179.95µs min=4µs      med=12µs    max=1.31s    p(90)=38µs     p(95)=68µs    
     http_req_sending...............: avg=59.27µs  min=1µs      med=3µs     max=1.18s    p(90)=11µs     p(95)=18µs    
     http_req_tls_handshaking.......: avg=0s       min=0s       med=0s      max=0s       p(90)=0s       p(95)=0s      
     http_req_waiting...............: avg=50.8ms   min=774µs    med=24.01ms max=3.23s    p(90)=125.65ms p(95)=211.32ms
     http_reqs......................: 744886 6200.648654/s
     iteration_duration.............: avg=63.2ms   min=822.83µs med=26.84ms max=3.76s    p(90)=164.36ms p(95)=250.36ms
     iterations.....................: 744886 6200.648654/s
     vus............................: 2      min=2                  max=999 
     vus_max........................: 1000   min=1000               max=1000

postgres:

     checks.........................: 48.35% 9135 out of 18892
     data_received..................: 5.4 MB 45 kB/s
     data_sent......................: 1.1 MB 8.8 kB/s
     http_req_blocked...............: avg=58.05µs min=0s      med=4µs   max=19.05ms p(90)=196µs   p(95)=356µs 
     http_req_connecting............: avg=44.17µs min=0s      med=0s    max=18.98ms p(90)=146.5µs p(95)=262µs 
     http_req_duration..............: avg=5.54s   min=11.65ms med=4.08s max=44.67s  p(90)=13.45s  p(95)=16.53s
       { expected_response:true }...: avg=5.51s   min=42.07ms med=4.08s max=44.67s  p(90)=13.47s  p(95)=16.48s
     http_req_failed................: 12.22% 1155 out of 9446
     http_req_receiving.............: avg=85.73µs min=6µs     med=61µs  max=3.96ms  p(90)=165.5µs p(95)=208µs 
     http_req_sending...............: avg=22.77µs min=2µs     med=13µs  max=10.19ms p(90)=44µs    p(95)=70µs  
     http_req_tls_handshaking.......: avg=0s      min=0s      med=0s    max=0s      p(90)=0s      p(95)=0s    
     http_req_waiting...............: avg=5.54s   min=11.61ms med=4.08s max=44.67s  p(90)=13.45s  p(95)=16.53s
     http_reqs......................: 9446   78.716602/s
     iteration_duration.............: avg=5.54s   min=11.73ms med=4.08s max=44.67s  p(90)=13.45s  p(95)=16.53s
     iterations.....................: 9446   78.716602/s
     vus............................: 2      min=2             max=1000
     vus_max........................: 1000   min=1000          max=1000
    
* */
