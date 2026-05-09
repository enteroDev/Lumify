import type { NextConfig } from "next";
import path from "path";

const nextConfig: NextConfig = {
  turbopack: {
    rules: {
      "*.svg": {
        loaders: [
          path.join(process.cwd(), "node_modules", "@svgr", "webpack", "dist", "index.js"),
        ],
        as: "*.js",
      },
    },
  },

  async rewrites() {
    return [
      {
        source: "/api/:path*",
        destination: "http://localhost:5331/:path*",
      },
      {
        source: "/Data/avatars/:path*",
        destination: "http://localhost:5331/Data/avatars/:path*",
      },
    ];
  },

};

export default nextConfig;