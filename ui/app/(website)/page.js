import HomePage from "./home";
import { allPosts} from "@/services/posts";

export default async function IndexPage() {
  const res = await  allPosts();
  console.log(res)
  return <HomePage posts={res.items} />;
}

// export const revalidate = 60;
