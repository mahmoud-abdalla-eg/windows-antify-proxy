import React from 'react';
import { 
  Shield, 
  Zap, 
  Terminal, 
  ExternalLink,
  CheckCircle2
} from 'lucide-react';

const GITHUB_REPO_URL = "https://github.com/mahmoud-abdalla-eg/windows-antify-proxy";
const GITHUB_ISSUES_URL = "https://github.com/mahmoud-abdalla-eg/windows-antify-proxy/issues";

function GithubIcon({ size = 18 }) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
      <path fillRule="evenodd" clipRule="evenodd" d="M12 2C6.477 2 2 6.484 2 12.017c0 4.425 2.865 8.18 6.839 9.504.5.092.682-.217.682-.483 0-.237-.008-.868-.013-1.703-2.782.605-3.369-1.343-3.369-1.343-.454-1.158-1.11-1.466-1.11-1.466-.908-.62.069-.608.069-.608 1.003.07 1.53 1.032 1.53 1.032.892 1.53 2.341 1.088 2.91.832.092-.647.35-1.088.636-1.338-2.22-.253-4.555-1.113-4.555-4.951 0-1.093.39-1.988 1.029-2.688-.103-.253-.446-1.272.098-2.65 0 0 .84-.27 2.75 1.026A9.564 9.564 0 0112 6.844c.85.004 1.705.115 2.504.337 1.909-1.296 2.747-1.027 2.747-1.027.546 1.379.202 2.398.1 2.651.64.7 1.028 1.595 1.028 2.688 0 3.848-2.339 4.695-4.566 4.943.359.309.678.92.678 1.855 0 1.338-.012 2.419-.012 2.747 0 .268.18.58.688.482A10.019 10.019 0 0022 12.017C22 6.484 17.522 2 12 2z" />
    </svg>
  );
}

export default function App() {
  return (
    <div className="min-h-screen">
      {/* Navigation */}
      <header className="navbar">
        <div className="nav-container">
          <a href="#" className="brand">
            <img src="/logo.png" alt="Win Antify Proxy" className="brand-logo" />
            <span>Win Antify Proxy</span>
            <span className="brand-badge">Windows</span>
          </a>

          <nav className="nav-links">
            <a href="#features" className="nav-link">Features</a>
            <a href="#how-it-works" className="nav-link">How It Works</a>
            <a href={GITHUB_ISSUES_URL} target="_blank" rel="noreferrer" className="nav-link">
              Issues
            </a>
          </nav>

          <div className="nav-actions">
            <a 
              href={GITHUB_REPO_URL} 
              target="_blank" 
              rel="noreferrer" 
              className="btn-primary"
              style={{ padding: '0.45rem 1.05rem', fontSize: '0.84rem' }}
            >
              <GithubIcon size={16} />
              View on GitHub
            </a>
          </div>
        </div>
      </header>

      {/* Hero Section */}
      <section className="hero">
        <div className="hero-glow"></div>
        <div className="hero-content">
          <div className="hero-pill">
            <Shield size={14} />
            <span>Windows 10 &amp; 11 Native · Per-App Environment Isolation</span>
          </div>

          <h1 className="hero-title">
            Proxy only the apps you choose <span>on Windows.</span>
          </h1>

          <p className="hero-subtitle">
            Win Antify Proxy is a lightweight, per-process transparent proxy utility for Windows. 
            Isolate proxy environment variables (<code>HTTP_PROXY</code>, <code>HTTPS_PROXY</code>) strictly to designated apps 
            like <strong>Antigravity</strong>—without polluting system variables, breaking online games, or installing heavy TUN drivers.
          </p>

          <div className="hero-actions">
            <a 
              href={GITHUB_REPO_URL} 
              target="_blank" 
              rel="noreferrer" 
              className="btn-primary" 
              style={{ fontSize: '1.05rem', padding: '0.85rem 1.8rem' }}
            >
              <GithubIcon size={18} />
              View on GitHub
            </a>
            <a 
              href={GITHUB_ISSUES_URL} 
              target="_blank" 
              rel="noreferrer" 
              className="btn-secondary" 
              style={{ fontSize: '1.05rem', padding: '0.85rem 1.8rem' }}
            >
              <ExternalLink size={16} />
              Feedback &amp; Issues
            </a>
          </div>

          <div className="hero-meta">
            Windows 10 &amp; 11 (64-bit) · 100% Free &amp; Open Source on GitHub
          </div>
        </div>

        {/* Real App Screenshot Showcase */}
        <div className="mockup-container">
          <div className="window-mockup">
            <div className="mockup-titlebar">
              <div className="mockup-titlebar-left">
                <img src="/logo.png" alt="" className="mockup-icon" />
                <span>Windows Antify Proxy — Per-App Isolated Proxy &amp; Environment Manager</span>
              </div>
              <div className="mockup-caption-buttons">
                <span>─</span>
                <span>□</span>
                <span>✕</span>
              </div>
            </div>
            <div className="mockup-image-frame">
              <img 
                src="/app-screenshot.png" 
                alt="Win Antify Proxy App Screenshot with Antigravity Rule" 
              />
            </div>
          </div>
        </div>
      </section>

      {/* Core Features */}
      <section id="features" className="section">
        <div className="section-header">
          <div className="section-tag">Key Advantages</div>
          <h2 className="section-title">Built specifically for Windows developers</h2>
          <p className="section-desc">
            Stop breaking your system with global environment variables. Isolate network settings per executable.
          </p>
        </div>

        <div className="grid-3">
          <div className="feature-card">
            <div className="feature-icon-box" style={{ background: 'rgba(56, 189, 248, 0.15)', color: '#38BDF8' }}>
              <Shield size={22} />
            </div>
            <h3 className="feature-title">Per-Process Isolation</h3>
            <p className="feature-text">
              Injects <code>HTTP_PROXY</code> and <code>HTTPS_PROXY</code> (e.g. <code>http://127.0.0.1:7897</code>) strictly into target processes. The rest of your Windows system stays untouched.
            </p>
          </div>

          <div className="feature-card">
            <div className="feature-icon-box" style={{ background: 'rgba(192, 132, 252, 0.15)', color: '#C084FC' }}>
              <Zap size={22} />
            </div>
            <h3 className="feature-title">Multi-Protocol &amp; Custom Env</h3>
            <p className="feature-text">
              Seamlessly handles HTTP, HTTPS, and SOCKS5 proxy protocols. Easily inject custom environment variables, arguments, and rule presets per application.
            </p>
          </div>

          <div className="feature-card">
            <div className="feature-icon-box" style={{ background: 'rgba(52, 211, 153, 0.15)', color: '#34D399' }}>
              <Terminal size={22} />
            </div>
            <h3 className="feature-title">Zero TUN / Kernel Bloat</h3>
            <p className="feature-text">
              No virtual TAP/TUN network adapters, no driver crashes, and no anti-cheat interference. Operates safely in user space with instant startup.
            </p>
          </div>
        </div>
      </section>

      {/* Three-Step Setup */}
      <section id="how-it-works" className="section">
        <div className="section-header">
          <div className="section-tag">Workflow</div>
          <h2 className="section-title">Three-step effortless setup</h2>
          <p className="section-desc">
            Get up and running in less than 30 seconds with no reboot or driver installation.
          </p>
        </div>

        <div className="step-grid">
          <div className="step-card">
            <div className="step-number">01</div>
            <h3 style={{ fontSize: '1.2rem', fontWeight: 700, marginBottom: '0.6rem' }}>Configure Your Proxy</h3>
            <p style={{ color: 'var(--text-secondary)', fontSize: '0.92rem' }}>
              Set your local proxy server (defaults to <code>127.0.0.1:7897</code> HTTP for Clash/Verge). Use built-in ping checking to verify latency.
            </p>
          </div>

          <div className="step-card">
            <div className="step-number">02</div>
            <h3 style={{ fontSize: '1.2rem', fontWeight: 700, marginBottom: '0.6rem' }}>Target Antigravity</h3>
            <p style={{ color: 'var(--text-secondary)', fontSize: '0.92rem' }}>
              Antigravity is auto-detected on your system. Set rule mode to <strong>Proxy</strong> to isolate network requests immediately.
            </p>
          </div>

          <div className="step-card">
            <div className="step-number">03</div>
            <h3 style={{ fontSize: '1.2rem', fontWeight: 700, marginBottom: '0.6rem' }}>Launch with Zero Leaks</h3>
            <p style={{ color: 'var(--text-secondary)', fontSize: '0.92rem' }}>
              Click <strong>Launch</strong> or use the 1-click desktop shortcut. Your app runs in an isolated environment block with complete privacy.
            </p>
          </div>
        </div>
      </section>

      {/* GitHub Call to Action Section */}
      <section className="section">
        <div className="download-card">
          <h2 style={{ fontSize: '2.2rem', fontWeight: 800, marginBottom: '0.8rem' }}>
            Open Source on GitHub
          </h2>
          <p style={{ color: 'var(--text-secondary)', fontSize: '1.05rem', maxWidth: '600px', margin: '0 auto 2rem' }}>
            Win Antify Proxy is free and open source. Explore the repository, view releases, star the project, or report an issue on GitHub.
          </p>

          <div style={{ display: 'flex', gap: '1rem', justifyContent: 'center', flexWrap: 'wrap' }}>
            <a 
              href={GITHUB_REPO_URL} 
              target="_blank" 
              rel="noreferrer" 
              className="btn-primary" 
              style={{ fontSize: '1.05rem', padding: '0.85rem 2rem' }}
            >
              <GithubIcon size={20} />
              Explore on GitHub
            </a>
            <a 
              href={GITHUB_ISSUES_URL} 
              target="_blank" 
              rel="noreferrer" 
              className="btn-secondary" 
              style={{ fontSize: '1.05rem', padding: '0.85rem 1.8rem' }}
            >
              <ExternalLink size={18} />
              Feedback &amp; Issues
            </a>
          </div>

          <div style={{ marginTop: '2rem', fontSize: '0.85rem', color: '#6B7280' }}>
            Windows 10 &amp; 11 (64-bit) • Powered by .NET 8 WPF • Open Source MIT
          </div>
        </div>
      </section>

      {/* Footer */}
      <footer className="footer">
        <div style={{ maxWidth: '1200px', margin: '0 auto', display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: '1rem' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.6rem' }}>
            <img src="/logo.png" alt="" className="footer-logo" />
            <span style={{ fontWeight: 600, color: '#FFFFFF' }}>Win Antify Proxy</span>
          </div>
          <div>
            Built with modern Windows isolation. Inspired by <a href="https://antifyapp.com/en/" target="_blank" rel="noreferrer" style={{ color: '#38BDF8' }}>Antify</a>.
          </div>
          <div style={{ display: 'flex', gap: '1.5rem', alignItems: 'center' }}>
            <a href={GITHUB_REPO_URL} target="_blank" rel="noreferrer" style={{ color: '#9499AD' }}>
              GitHub
            </a>
            <a href={GITHUB_ISSUES_URL} target="_blank" rel="noreferrer" style={{ color: '#9499AD' }}>
              Feedback &amp; Issues
            </a>
          </div>
        </div>
      </footer>
    </div>
  );
}
