import PostPage from "./default";

import { allPostSlug, postBySlug } from "@/services/posts";

export async function generateStaticParams() {
  return await allPostSlug();
}

export async function generateMetadata({ params }) {
  const post = await postBySlug(params.slug);
  return { title: post.title };
}

export default async function PostDefault({ params }) {
  const post = await postBySlug(params.slug);
  return <PostPage post={post} />;
}

// export const revalidate = 60;
