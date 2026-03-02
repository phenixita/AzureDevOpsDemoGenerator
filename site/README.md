# Astro GitHub Pages Site

This folder contains the Astro-powered GitHub Pages site for the Azure DevOps Demo Generator repository.

## Commands

- `npm install`: install dependencies
- `npm run dev`: run local site at `http://localhost:4321`
- `npm run build`: create static output in `site/dist`
- `npm run preview`: preview the production build

## Deployment

Deployment is handled by `.github/workflows/pages-astro.yml`, which builds this `site/` project and publishes it to GitHub Pages.
