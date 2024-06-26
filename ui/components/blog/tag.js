import Link from "next/link";
import Label from "@/components/ui/label";

export default function TagLabel({
  tags,
  nomargin = false
}) {
  return (
    <div className="flex gap-3">
      {tags?.length &&
        tags.map((tag, index) => (
          <Link
            href={`/category/${tag.slug}`}
            key={index}>
            <Label nomargin={nomargin} color={tag.color}>
              {tag.name}
            </Label>
          </Link>
        ))}
    </div>
  );
}
