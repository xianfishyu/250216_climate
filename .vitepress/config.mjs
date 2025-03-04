import { defineConfig } from 'vitepress';

// 自动更新类引用
import fs from 'fs';
import path from 'path';

// 获取所有 C# 文件中的类名
const scriptsDir = path.join(__dirname, '..', 'Scripts')
const classNames = []

fs.readdirSync(scriptsDir).forEach(file => {
    if (file.endsWith('.cs')) {
        const className = file.replace('.cs', '')
        classNames.push(className)
    }
})

const items = classNames.map(className => ({
    text: className,
    link: `/docs/class_reference/${className}`
}))

// https://vitepress.dev/reference/site-config
export default defineConfig({
    base: '/250216_climate/',
    srcDir: "web/src",
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
                text: "待办事项",
                collapsed: false,
                link: "/todo/todolist"
            },
            {
                text: "开发文档",
                collapsed: false,
                items: [
                    { text: "介绍", link: "/docs/describe" },
                    {
                        text: "类引用", link: "/docs/class_reference", items: [
                            ...items
                        ]
                    }
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
