import { defineConfig } from "vitepress";


// https://vitepress.dev/reference/site-config
export default defineConfig({
    srcDir: ".vitepress/src",
    lang: "zh-Hans",
    title: "250216_climate",
    description: "不知道",
    sitemap: {
        hostname: "https://xianfishyu.github.io/250216_climate"
    },
    themeConfig: {
        // https://vitepress.dev/reference/default-theme-config
        nav: [
            { text: "首页", link: "/" },
            { text: "开发文档", link: "/docs/introduction", activeMatch: "/docs" }
        ],
        sidebar: [
            {
                text: "开始使用",
                collapsed: false,
                items: [
                ]
            },
            {
                text: "开发文档",
                collapsed: false,
                items: [
                ]
            }
        ],
        editLink: {
        },
    
        lastUpdated: {
            text: "最后更新",
            formatOptions: {
                dateStyle: "medium",
                timeStyle: "short"
            }
        },
        docFooter: {
            prev: "上一页",
            next: "下一页"
        },
        search: {
            provider: "local",
        },
        footer: {
        },
        externalLinkIcon: true
    }
});
