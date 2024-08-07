import { fetcher, fullApiPath } from "@/services/config";

export async function allPosts(params) {
  const query = params ? new URLSearchParams(params).toString():'';
  let path = fullApiPath(`/views/latest-posts?` + query);
  return await fetcher(path);
}

export async function allPostSlug(){
  const path = fullApiPath('/views/latest-post-slugs/many');
  return await fetcher(path);
}

export async function postBySlug(slug){
  const path = fullApiPath('/views/post-by-slug/one?slug='+slug);
  return await fetcher(path);
}