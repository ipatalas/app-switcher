import fs from 'node:fs';

const frontmatter = `---
title: Changelog
description: Latest updates and changes
---
import { Badge } from '@astrojs/starlight/components';

`;

const regex = /## \[(\d+\.\d+\.\d+)\] - (\d{4}-\d{2}-\d{2})/g;
let changelog = fs.readFileSync('../CHANGELOG.md', 'utf-8')
    .replace(/.*?(?=## \[\d)/s, '')
    .replaceAll(regex, '## v$1 <Badge text="$2" variant="note" />');

fs.writeFileSync('./src/content/docs/reference/changelog.mdx', frontmatter + changelog);
console.log('✅ Changelog synced!');