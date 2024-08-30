import { fetcher, fullApiPath } from "@/services/config";

export async function allPosts(params) {
  const query = params ? new URLSearchParams(params).toString():'';
  let path = fullApiPath(`/queries/latest-posts?` + query);
  return await fetcher(path);
}

export async function allPostSlug(){
  const path = fullApiPath('/queries/latest-post-slugs/many');
  return await fetcher(path);
}

export async function postBySlug(slug){
  const path = fullApiPath('/queries/post-by-slug/one?slug='+slug);
  console.log({path});
  return await fetcher(path);
}