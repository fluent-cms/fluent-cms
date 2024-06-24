import { fetcher, fullPath } from "@/services/config";

export async function allPosts(){
  const path = fullPath('/views/latest-posts');
  return await fetcher(path);
}