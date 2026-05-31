import {themes as prismThemes} from 'prism-react-renderer';
import type {Config} from '@docusaurus/types';
import type * as Preset from '@docusaurus/preset-classic';

const config: Config = {
  title: 'dotnet-to-typescript',
  tagline: 'Generate reliable TypeScript definitions from your .NET assemblies.',
  favicon: 'img/favicon.ico',

  future: {
    v4: true,
  },

  url: 'https://mzbrau.github.io',
  baseUrl: '/dotnet-to-typescript/',

  organizationName: 'mzbrau',
  projectName: 'dotnet-to-typescript',

  onBrokenLinks: 'throw',

  i18n: {
    defaultLocale: 'en',
    locales: ['en'],
  },

  presets: [
    [
      'classic',
      {
        docs: {
          path: '../docs',
          routeBasePath: 'docs',
          sidebarPath: './sidebars.ts',
          editUrl:
            'https://github.com/mzbrau/dotnet-to-typescript/tree/main/docs/',
        },
        blog: false,
        theme: {
          customCss: './src/css/custom.css',
        },
      } satisfies Preset.Options,
    ],
  ],

  themeConfig: {
    image: 'img/docusaurus-social-card.jpg',
    colorMode: {
      respectPrefersColorScheme: true,
    },
    navbar: {
      title: 'dotnet-to-typescript',
      logo: {
        alt: 'dotnet-to-typescript logo',
        src: 'img/logo.svg',
      },
      items: [
        {
          type: 'docSidebar',
          sidebarId: 'tutorialSidebar',
          position: 'left',
          label: 'Docs',
        },
        {
          href: 'https://github.com/mzbrau/dotnet-to-typescript',
          label: 'GitHub',
          position: 'right',
        },
      ],
    },
    footer: {
      style: 'dark',
      links: [
        {
          title: 'Documentation',
          items: [
            {
              label: 'Overview',
              to: '/docs/overview',
            },
            {
              label: 'CLI Reference',
              to: '/docs/cli-reference',
            },
          ],
        },
        {
          title: 'Project',
          items: [
            {
              label: 'Source Repository',
              href: 'https://github.com/mzbrau/dotnet-to-typescript',
            },
            {
              label: 'NuGet Package',
              href: 'https://www.nuget.org/packages/dotnet-to-typescript/',
            },
          ],
        },
      ],
      copyright: `Copyright © ${new Date().getFullYear()} dotnet-to-typescript contributors.`,
    },
    prism: {
      theme: prismThemes.github,
      darkTheme: prismThemes.dracula,
    },
  } satisfies Preset.ThemeConfig,
};

export default config;
