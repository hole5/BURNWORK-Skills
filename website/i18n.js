/* Shared ZH/EN toggle for BURNWORK site */
(function () {
  const KEY = "burnwork-lang";

  const dict = {
    zh: {
      "meta.title": "燃烧工作室 · 本机 AI 短剧创作工作台",
      "meta.desc": "燃烧工作室（BURNWORK）：本机短剧工作台。技能负责理解与编排，ComfyUI 负责出图出视频，五份 Markdown 与素材留在你的设备里。",
      "brand.name": "燃烧工作室",
      "nav.stack": "产品能力",
      "nav.flow": "工作流",
      "nav.pricing": "获取方式",
      "nav.support": "更新与支持",
      "nav.label": "主导航",
      "action.skills": "技能库",
      "action.open": "打开工作室",
      "hero.eyebrow": "BURNWORK STUDIO · 本机短剧工作台",
      "hero.line1": "把一句想法，",
      "hero.burn": "烧成",
      "hero.line2": "可继续拍的镜头",
      "hero.lede": "本机技能理解原文与创意，拆解剧本、角色与分镜；自部署 ComfyUI 承接图像与视频。项目、素材和创作记录留在你的设备里。",
      "hero.cta1": "查看启动方式",
      "hero.cta2": "了解工作流",
      "hero.meta": "Windows 独立窗口 · 本机优先 · 你的设备，你的数据",
      "stage.home": "首页",
      "stage.projects": "项目",
      "stage.settings": "设置",
      "stage.quote": "把脑海里的画面，交给 BURNWORK 拍出来",
      "stage.input": "输入镜头、画面或故事…",
      "stage.cap": "真实工作台气质 · 本机运行",
      "stack.h2": "一套本机可控的短剧生产三件套",
      "stack.lede": "不是把功能堆在一起，而是让故事、资产和生成任务在同一个项目里持续向前。",
      "stack.s.h": "技能创作中枢",
      "stack.s.p": "在本机理解原文与创意，拆解角色、剧本和分镜，并把每一步组织成可继续执行的任务。",
      "stack.s.1": "创意与小说解析",
      "stack.s.2": "剧本和分镜编排",
      "stack.s.3": "可检查、可迭代的任务流",
      "stack.c.h": "ComfyUI 生成引擎",
      "stack.c.p": "连接你配置的自部署 ComfyUI，统一承接角色、美术、分镜图片和视频生成。",
      "stack.c.1": "参考图与资产引用",
      "stack.c.2": "图片、视频任务预览",
      "stack.c.3": "确认后再投产",
      "stack.a.h": "五文档资产沉淀",
      "stack.a.p": "每集五份 Markdown 就是创作事实；素材、引用与生成记录持续沉淀，跨镜头复用。",
      "stack.a.1": "角色与美术设定",
      "stack.a.2": "分镜与提示词可追溯",
      "stack.a.3": "本机项目数据可控",
      "flow.h2": "从一句想法，到可继续生产的镜头",
      "flow.lede": "每一步都有明确的数据、状态和下一步，不再靠散落的提示词维持项目。",
      "flow.1.h": "给出原文或创意",
      "flow.1.p": "导入小说，或从一句镜头、一段故事开始；也可拖入参考图。",
      "flow.2.h": "确认方案再落稿",
      "flow.2.p": "先出创作计划，再写剧本、角色、美术和镜头结构。",
      "flow.3.h": "资产进入项目",
      "flow.3.p": "角色、场景、道具与参考图形成可复用资产，连续性写进文件。",
      "flow.4.h": "确认后生成素材",
      "flow.4.p": "图片和视频先预览参数与路径，明确确认后才交给 ComfyUI。",
      "board.1.h": "分镜",
      "board.1.p": "景别 · 轴线 · 冻结关键帧",
      "board.2.h": "提示词",
      "board.2.p": "IMG · MOTION · 可复制",
      "board.3.h": "生产",
      "board.3.p": "预览 → 确认 → 出图出视频",
      "board.cap": "分镜、生成状态和素材记录都在项目里",
      "local.h2": "本机优先，也持续进化",
      "local.lede": "落在自己设备上的工作台：素材与创作记录默认不出境。技能库与工作室壳会继续迭代工作流和模型接入。",
      "local.1": "本地运行",
      "local.2": "数据留在本机",
      "local.3": "先确认，再生产",
      "pricing.h2": "获取方式清晰，费用边界清楚",
      "pricing.lede": "核心工作台可按仓库自行编译使用；你连接的模型与算力费用由对应服务商收取。若日后提供付费支持包，购买页会单独标明。",
      "pricing.self.h": "自建使用",
      "pricing.self.p": "MIT 上游 + 本机工作室壳。适合独立创作者把生产线留在自己设备上。",
      "pricing.self.1": "完整本机工作台能力",
      "pricing.self.2": "技能编排与五文档项目",
      "pricing.self.3": "自部署 ComfyUI 出图出视频",
      "pricing.self.4": "项目与资产留在本机",
      "pricing.sup.h": "支持与定制",
      "pricing.sup.p": "需要协助接入、流程定制或团队落地时，可另行沟通范围与报价。",
      "pricing.sup.1": "工作流与模型接入协助",
      "pricing.sup.2": "合理范围内的需求沟通",
      "pricing.sup.3": "定制开发按约定交付",
      "pricing.sup.4": "适用服务条款与退款政策",
      "legal.line": "继续使用本站与软件即表示你同意 {terms}、{privacy} 与 {refund}，并知悉数字内容与开源许可的交付方式。",
      "legal.terms": "服务条款",
      "legal.privacy": "隐私政策",
      "legal.refund": "退款政策",
      "support.h2": "更新与支持",
      "support.lede": "工作流与模型接入会持续迭代。政策与许可说明可在下列页面查阅。",
      "support.studio": "工作室说明",
      "start.h2": "把下一部短剧，留在自己的工作台里",
      "start.lede": "Windows 推荐独立窗口；非 Windows 可用浏览器对照页。上游技能套件以 MIT 开源。",
      "start.win.h": "Windows 工作室",
      "start.win.p": "编译并打开本机窗口，浏览五文档、对话与确认后生产。",
      "start.web.h": "浏览器对照",
      "start.web.p": "同一套项目后端，适合非 Windows 或对照调试。",
      "start.doc": "阅读工作室说明",
      "start.product": "产品边界",
      "foot.tag": "本机 AI 短剧创作工作台 · 技能中枢 × 自部署 ComfyUI",
      "lang.zh": "中文",
      "lang.en": "EN",
      "page.back": "返回首页",
    },
    en: {
      "meta.title": "Burning Studio · Local AI Short-Drama Workbench",
      "meta.desc": "Burning Studio (BURNWORK): a local short-drama workbench. Skills handle understanding and orchestration; ComfyUI handles image and video. Markdown and assets stay on your machine.",
      "brand.name": "Burning Studio",
      "nav.stack": "Capabilities",
      "nav.flow": "Workflow",
      "nav.pricing": "Get started",
      "nav.support": "Updates & support",
      "nav.label": "Primary",
      "action.skills": "Skills",
      "action.open": "Open studio",
      "hero.eyebrow": "BURNWORK STUDIO · Local short-drama workbench",
      "hero.line1": "Turn one idea into",
      "hero.burn": "shots",
      "hero.line2": " you can keep producing",
      "hero.lede": "Local skills read your story and ideas, then break them into scripts, cast, and boards. Your own ComfyUI handles images and video. Projects, assets, and history stay on your device.",
      "hero.cta1": "See how to launch",
      "hero.cta2": "Explore the workflow",
      "hero.meta": "Windows desktop · Local-first · Your device, your data",
      "stage.home": "Home",
      "stage.projects": "Projects",
      "stage.settings": "Settings",
      "stage.quote": "Hand the picture in your head to BURNWORK",
      "stage.input": "Enter a shot, frame, or story…",
      "stage.cap": "Real workbench feel · Runs locally",
      "stack.h2": "A local production stack you control",
      "stack.lede": "Not a pile of features—story, assets, and generation keep moving inside one project.",
      "stack.s.h": "Skills hub",
      "stack.s.p": "Understand source material and ideas on-device, then turn cast, script, and boards into tasks you can continue.",
      "stack.s.1": "Idea & novel analysis",
      "stack.s.2": "Script and board planning",
      "stack.s.3": "Checkable, iterable task flow",
      "stack.c.h": "ComfyUI engine",
      "stack.c.p": "Connect your self-hosted ComfyUI for cast, art, stills, and video generation.",
      "stack.c.1": "Reference images & assets",
      "stack.c.2": "Image and video previews",
      "stack.c.3": "Generate only after confirm",
      "stack.a.h": "Five-document assets",
      "stack.a.p": "Five Markdown files per episode are the source of truth; assets and generation history accumulate for reuse.",
      "stack.a.1": "Cast and art setup",
      "stack.a.2": "Traceable boards & prompts",
      "stack.a.3": "Local project data you own",
      "flow.h2": "From one idea to shots you can produce",
      "flow.lede": "Every step has clear data, status, and a next move—no more scattered prompts holding the project together.",
      "flow.1.h": "Bring text or an idea",
      "flow.1.p": "Import a novel, or start from one shot or a short story. Drop reference images too.",
      "flow.2.h": "Confirm, then write",
      "flow.2.p": "Lock the plan first, then script, cast, art, and shot structure.",
      "flow.3.h": "Assets enter the project",
      "flow.3.p": "Characters, locations, props, and refs become reusable assets with continuity in files.",
      "flow.4.h": "Generate after confirm",
      "flow.4.p": "Preview paths and params for images and video, then send to ComfyUI only when you confirm.",
      "board.1.h": "Boards",
      "board.1.p": "Framing · axis · frozen frames",
      "board.2.h": "Prompts",
      "board.2.p": "IMG · MOTION · copy-ready",
      "board.3.h": "Produce",
      "board.3.p": "Preview → confirm → generate",
      "board.cap": "Boards, job status, and assets live in the project",
      "local.h2": "Local-first, still evolving",
      "local.lede": "A workbench on your machine: assets and records stay put by default. Skills and the studio shell keep iterating on workflows and model hooks.",
      "local.1": "Runs locally",
      "local.2": "Data stays on-device",
      "local.3": "Confirm before produce",
      "pricing.h2": "Clear access, clear cost boundaries",
      "pricing.lede": "Build the core workbench from the repo yourself. Model and compute fees go to the providers you connect. Paid support packs, if offered later, will be listed separately.",
      "pricing.self.h": "Self-hosted use",
      "pricing.self.p": "MIT upstream + local studio shell. Built for creators who keep the line on their own hardware.",
      "pricing.self.1": "Full local workbench",
      "pricing.self.2": "Skills + five-document projects",
      "pricing.self.3": "Self-hosted ComfyUI image & video",
      "pricing.self.4": "Projects and assets on-device",
      "pricing.sup.h": "Support & custom",
      "pricing.sup.p": "Need help wiring models, shaping workflows, or landing a team setup? Scope and pricing are discussed separately.",
      "pricing.sup.1": "Workflow and model integration help",
      "pricing.sup.2": "Reasonable product consultation",
      "pricing.sup.3": "Custom work by agreement",
      "pricing.sup.4": "Terms and refund policy apply",
      "legal.line": "By continuing to use this site and software, you agree to the {terms}, {privacy}, and {refund}, and understand how digital content and open-source licensing are delivered.",
      "legal.terms": "Terms of Service",
      "legal.privacy": "Privacy Policy",
      "legal.refund": "Refund Policy",
      "support.h2": "Updates & support",
      "support.lede": "Workflows and model hooks keep evolving. Policies and licenses are linked below.",
      "support.studio": "Studio guide",
      "start.h2": "Keep the next short drama on your own bench",
      "start.lede": "Windows prefers the desktop app; other platforms can use the browser counterpart. Upstream skills ship under MIT.",
      "start.win.h": "Windows studio",
      "start.win.p": "Build and open the desktop window to browse the five docs, chat, and confirm production.",
      "start.web.h": "Browser counterpart",
      "start.web.p": "Same project backend—useful off Windows or for side-by-side checks.",
      "start.doc": "Read the studio guide",
      "start.product": "Product boundaries",
      "foot.tag": "Local AI short-drama workbench · Skills hub × self-hosted ComfyUI",
      "lang.zh": "中文",
      "lang.en": "EN",
      "page.back": "Back to home",
    },
  };

  function lang() {
    const saved = localStorage.getItem(KEY);
    if (saved === "zh" || saved === "en") return saved;
    return "zh";
  }

  function t(key, locale) {
    const pack = dict[locale] || dict.zh;
    return pack[key] ?? dict.zh[key] ?? key;
  }

  function fillLegal(locale) {
    const template = t("legal.line", locale);
    const terms = `<a href="terms.html">${t("legal.terms", locale)}</a>`;
    const privacy = `<a href="privacy.html">${t("legal.privacy", locale)}</a>`;
    const refund = `<a href="refund.html">${t("legal.refund", locale)}</a>`;
    return template
      .replace("{terms}", terms)
      .replace("{privacy}", privacy)
      .replace("{refund}", refund);
  }

  function apply(locale) {
    document.documentElement.lang = locale === "en" ? "en" : "zh-CN";
    localStorage.setItem(KEY, locale);

    document.querySelectorAll("[data-i18n]").forEach((el) => {
      const key = el.getAttribute("data-i18n");
      if (!key) return;
      el.textContent = t(key, locale);
    });

    document.querySelectorAll("[data-i18n-html]").forEach((el) => {
      const key = el.getAttribute("data-i18n-html");
      if (key === "legal.line") el.innerHTML = fillLegal(locale);
      else el.innerHTML = t(key, locale);
    });

    document.querySelectorAll("[data-i18n-aria]").forEach((el) => {
      const key = el.getAttribute("data-i18n-aria");
      if (key) el.setAttribute("aria-label", t(key, locale));
    });

    const titleEl = document.querySelector("title[data-i18n]");
    if (titleEl) {
      document.title = t(titleEl.getAttribute("data-i18n"), locale);
    }

    const meta = document.querySelector('meta[name="description"]');
    if (meta && (location.pathname.endsWith("index.html") || /\/website\/?$/.test(location.pathname))) {
      meta.setAttribute("content", t("meta.desc", locale));
    }

    document.querySelectorAll(".lang-switch [data-lang]").forEach((btn) => {
      btn.classList.toggle("is-active", btn.getAttribute("data-lang") === locale);
      btn.setAttribute("aria-pressed", btn.getAttribute("data-lang") === locale ? "true" : "false");
    });
  }

  function bind() {
    document.querySelectorAll(".lang-switch [data-lang]").forEach((btn) => {
      btn.addEventListener("click", () => apply(btn.getAttribute("data-lang")));
    });
  }

  window.BurnworkI18n = { apply, lang, t, bind };

  document.addEventListener("DOMContentLoaded", () => {
    bind();
    apply(lang());
  });
})();
