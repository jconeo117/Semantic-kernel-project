/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  darkMode: 'class',
  theme: {
    extend: {
      fontFamily: {
        display: ['Syne', 'sans-serif'],
        body: ['DM Sans', 'sans-serif'],
      },
      colors: {
        bg: 'var(--bg)',
        surface: 'var(--surface)',
        surface2: 'var(--surface2)',
        border: 'var(--border)',
        'text-primary': 'var(--text-primary)',
        'text-secondary': 'var(--text-secondary)',
        'text-muted': 'var(--text-muted)',
        accent: 'var(--accent)',
        'accent-light': 'var(--accent-light)',
        'accent-hover': 'var(--accent-hover)',
        green: 'var(--green)',
        amber: 'var(--amber)',
        red: 'var(--red)',
        slate: {
          850: '#151e2e',
          950: '#0b0f19'
        }
      }
    },
  },
  plugins: [],
}
