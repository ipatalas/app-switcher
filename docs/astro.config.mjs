import { defineConfig } from 'astro/config';
import starlight from '@astrojs/starlight';

export default defineConfig({
	outDir: '../website/docs',
	base: '/docs',
	integrations: [
		starlight({
			title: 'AppSwitcher',
			description: 'Documentation for AppSwitcher — keyboard-driven window switching for Windows.',
			logo: {
				src: '../website/app-switcher.png',
			},
			// social: [
			// 	{ icon: 'github', label: 'GitHub', href: 'https://github.com/ipatalas/app-switcher' },
			// ],
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
						{ label: 'Assigning Hotkeys', slug: 'configuration/hotkeys' },
						{ label: 'Cycle Modes', slug: 'configuration/cycle-modes' },
						{ label: 'Startup & Tray', slug: 'configuration/startup' },
					],
				},
				{
					label: 'Advanced',
					items: [
						{ label: 'Running as Administrator', slug: 'advanced/admin' },
						{ label: 'Portable Mode', slug: 'advanced/portable' },
					],
				},
				{
					label: 'Reference',
					items: [
						{ label: 'CLI Commands', slug: 'reference/cli' },
						{ label: 'Troubleshooting', slug: 'reference/troubleshooting' },
						{ label: 'Changelog', slug: 'reference/changelog' },
					],
				},
			],
		}),
	],
});