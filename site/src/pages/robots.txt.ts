export function GET() {
	const site = import.meta.env.SITE || 'https://example.com';
	const basePath = import.meta.env.BASE_URL || '/';
	const sitemapUrl = new URL(`${basePath}sitemap-index.xml`, site).toString();
	const body = `User-agent: *\nAllow: /\n\nSitemap: ${sitemapUrl}\n`;

	return new Response(body, {
		headers: {
			'Content-Type': 'text/plain; charset=utf-8',
		},
	});
}
