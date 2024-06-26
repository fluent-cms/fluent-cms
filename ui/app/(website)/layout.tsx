import Footer from "@/components/footer";
import Navbar from "@/components/navbar";

export default async function Layout({ children, params }) {
  return (
    <>
      <Navbar/>

      <div>{children}</div>

      <Footer />
    </>
  );
}
// enable revalidate for all pages in this layout
// export const revalidate = 60;
