/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{ts,tsx}'],
  theme: {
    extend: {
      colors: {
        // Enterprise dark SaaS palette matching the Figma reference screens.
        page: '#0a0d16',
        sidebar: '#0d1120',
        topbar: '#0d1120',
        card: '#12162a',
        cardMuted: '#161b32',
        border: {
          DEFAULT: '#22273d',
          light: '#2b3151'
        },
        primary: {
          DEFAULT: '#3b82f6',
          hover: '#2563eb',
          muted: '#1d3a68'
        },
        risk: {
          critical: '#ef4444',
          riskyBg: '#78350f',
          risky: '#f59e0b',
          safe: '#22c55e',
          unknown: '#64748b'
        },
        text: {
          primary: '#f1f5f9',
          secondary: '#94a3b8',
          muted: '#64748b'
        }
      },
      fontFamily: {
        sans: ['Inter', 'ui-sans-serif', 'system-ui', 'sans-serif'],
        mono: ['"JetBrains Mono"', 'ui-monospace', 'SFMono-Regular', 'monospace']
      },
      keyframes: {
        fadeIn: {
          from: { opacity: 0, transform: 'translateY(0.5rem)' },
          to: { opacity: 1, transform: 'translateY(0)' }
        },
        pulseGlow: {
          '0%, 100%': { opacity: 1 },
          '50%': { opacity: 0.55 }
        }
      },
      animation: {
        fadeIn: 'fadeIn 0.25s ease',
        pulseGlow: 'pulseGlow 1.6s ease-in-out infinite'
      }
    }
  },
  plugins: []
};

