import { defineConfig } from 'astro/config';
import starlight from '@astrojs/starlight';

export default defineConfig({
	site: 'https://app-switcher.com',
	outDir: '../website/docs',
	base: '/docs',
	redirects: {
		'/': '/docs/getting-started/installation',
		'/docs': '/docs/getting-started/installation'
	},
	vite: {
		resolve: {
			alias: {
				'@components': '/src/components',
				'@assets': '/src/assets'
			}
		}
	},
	integrations: [
		starlight({
			title: 'AppSwitcher',
			description: 'Documentation for AppSwitcher — keyboard-driven window switching for Windows.',
			logo: {
				src: '../website/app-switcher.png',
			},
			favicon: 'app-switcher.png',
			components: {
				SocialIcons: './src/components/SocialIcons.astro',
				Pagination: './src/components/Pagination.astro',
			},
			customCss: [
				'./src/styles/custom.css',
			],
			sidebar: [
				{
					label: 'Getting Started',
					items: [
						{ label: 'Installation', slug: 'getting-started/installation' },
						{ label: 'Quick Start', slug: 'getting-started/quick-start' },
						{ label: 'System Requirements', slug: 'getting-started/requirements' },
					],
				},
				{
					label: 'Configuration',
					items: [
						{ label: 'Choosing a Modifier Key', slug: 'configuration/choosing-modifier' },
					{ label: 'Hotkeys (manual)', slug: 'configuration/hotkeys' },
					{ label: 'Dynamic Mode (automatic)', slug: 'configuration/dynamic-mode' },
				{ label: 'Cycle Modes', slug: 'configuration/cycle-modes' },
					{ label: 'Peek Mode', slug: 'configuration/peek-mode' },
					{ label: 'Startup & Tray', slug: 'configuration/startup' },
					],
				},
				{
					label: 'Advanced',
					items: [
						{ label: 'Elevated Apps', slug: 'advanced/elevated-apps' },
						{ label: 'Firewall setup', slug: 'advanced/firewall' },
						{ label: 'Portable Mode', slug: 'advanced/portable' },
					],
				},
				{
					label: 'Reference',
					items: [
						{ label: 'Troubleshooting', slug: 'reference/troubleshooting' },
						{ label: 'Changelog', slug: 'reference/changelog' },
					],
				},
				{
					label: 'Insights',
					items: [
						{ label: 'Statistics & Grading', slug: 'insights/statistics' },
					],
				},
			],
		}),
	],
});