import Image from "next/image";
import Link from "next/link";
import Container from "@/components/container";
import { notFound } from "next/navigation";
import { parseISO, format } from "date-fns";

import TagLabel from "@/components/blog/tag";
import AuthorCard from "@/components/blog/authorCard";
import { fullFilePath } from "@/services/config";

export default function Post(props) {
  const { loading, post } = props;

  const slug = post?.slug;

  if (!loading && !slug) {
    notFound();
  }

  return (
    <>
      <Container className="!pt-0">
        <div className="mx-auto max-w-screen-md ">
          <div className="flex justify-center">
            <TagLabel tags={post.tags} />
          </div>

          <h1 className="text-brand-primary mb-3 mt-2 text-center text-3xl font-semibold tracking-tight dark:text-white lg:text-4xl lg:leading-snug">
            {post.title}
          </h1>

          <div className="mt-3 flex justify-center space-x-3 text-gray-500 ">
            <div className="flex items-center gap-3">
              {post.authors?.map((author) => (
                <>
                  <div key={author.id} className="relative h-10 w-10 flex-shrink-0">
                    <Link href={`/author/${author.slug}`}>
                      <Image
                        src={fullFilePath(author.thumbnail_image)}
                        alt={author.name}
                        className="rounded-full object-cover"
                        fill
                        sizes="40px"
                      />
                    </Link>
                  </div>
                  <div className="flex items-center space-x-2 text-sm">
                    <p className="text-gray-800 dark:text-gray-400">
                      <Link href={`/author/${author.slug}`}> {author.name} </Link>
                    </p>
                  </div>
                </>
              ))}
              <div>
                <div className="flex items-center space-x-2 text-sm">
                  <time
                    className="text-gray-500 dark:text-gray-400"
                    dateTime={post?.published_at || post.created_at}>
                    {format(
                      parseISO(post?.published_at || post.created_at),
                      "MMMM dd, yyyy"
                    )}
                  </time>
                  <span>· {post.reading_time} to read</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </Container>

      <div className="relative z-0 mx-auto aspect-video max-w-screen-lg overflow-hidden lg:rounded-lg">
          <Image
            fill
            src={fullFilePath(post.featured_image)}
            alt={"Thumbnail"}
            loading="eager"
            sizes="100vw"
            className="object-cover"
          />
      </div>

      <Container>
        <article className="mx-auto max-w-screen-md ">
          <div className="prose mx-auto my-3 dark:prose-invert prose-a:text-blue-600">
            <div dangerouslySetInnerHTML={{__html:post.content}}/>
          </div>
          <div className="mb-7 mt-7 flex justify-center">
            <Link
              href="/"
              className="bg-brand-secondary/20 rounded-full px-5 py-2 text-sm text-blue-600 dark:text-blue-500 ">
              ← View all posts
            </Link>
          </div>
          {
            post.authors?.map(author=><AuthorCard key={author.id} author={author} />)
          }
        </article>
      </Container>
    </>
  );
}