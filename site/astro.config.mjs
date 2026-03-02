// @ts-check
import { defineConfig } from 'astro/config';
import sitemap from '@astrojs/sitemap';

const repository = process.env.GITHUB_REPOSITORY ?? '';
const repoName = repository.includes('/') ? repository.split('/')[1] : '';
const isPagesDomainRepo = repoName === `${process.env.GITHUB_REPOSITORY_OWNER}.github.io`;
const base =
	process.env.BASE_PATH ?? (repoName && !isPagesDomainRepo ? `/${repoName}` : '/');
const site =
	process.env.SITE_URL ??
	(process.env.GITHUB_REPOSITORY_OWNER
		? `https://${process.env.GITHUB_REPOSITORY_OWNER}.github.io`
		: 'https://example.com');

export default defineConfig({
	site,
	base,
	output: 'static',
	trailingSlash: 'always',
	integrations: [sitemap()],
});
