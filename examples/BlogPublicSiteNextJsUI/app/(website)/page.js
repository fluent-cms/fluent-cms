import HomePage from "./home";
import { allPosts} from "@/services/posts";

export default async function IndexPage() {
  const res = await  allPosts();
  return <HomePage posts={res.items} />;
}

// export const revalidate = 60;
