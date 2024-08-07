import PostList from "@/components/postlist";
import Pagination from "@/components/blog/pagination";
import { allPosts } from "@/services/posts";

export default async function Post({ searchParams }) {
  searchParams.limit = 6;
  const {items:posts, first, last} = await allPosts(searchParams);
  return (
    <>
      {posts && posts?.length === 0 && (
        <div className="flex h-40 items-center justify-center">
          <span className="text-lg text-gray-500">
            End of the result!
          </span>
        </div>
      )}
      <div className="mt-10 grid gap-10 md:grid-cols-2 lg:gap-10 xl:grid-cols-3">
        {posts.map(post => (
          <PostList key={post.id} post={post} aspect="square" />
        ))}
      </div>

      <Pagination first={first} last={last} />
    </>
  );
}
