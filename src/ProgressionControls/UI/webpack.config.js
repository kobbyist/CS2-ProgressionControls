const fs = require("fs");
const path = require("path");
const MiniCssExtractPlugin = require("mini-css-extract-plugin");
const TerserPlugin = require("terser-webpack-plugin");
const mod = require("./mod.json");

const userDataPath = process.env.CSII_USERDATAPATH;
if (!userDataPath) {
  throw new Error(
    "CSII_USERDATAPATH is not set. Install or repair the CS2 modding toolchain."
  );
}

const outputPath = path.join(userDataPath, "Mods", mod.id);

class CopyManifestPlugin {
  apply(compiler) {
    compiler.hooks.afterEmit.tap("CopyManifestPlugin", () => {
      fs.copyFileSync(
        path.resolve(__dirname, "mod.json"),
        path.join(outputPath, "mod.json")
      );
    });
  }
}

module.exports = {
  mode: "production",
  entry: {
    [mod.id]: "./src/index.tsx"
  },
  externalsType: "window",
  externals: {
    react: "React",
    "react-dom": "ReactDOM",
    "cs2/api": "cs2/api",
    "cs2/l10n": "cs2/l10n",
    "cs2/modding": "cs2/modding"
  },
  module: {
    rules: [
      {
        test: /\.tsx?$/,
        use: "ts-loader",
        exclude: /node_modules/
      },
      {
        test: /\.s?css$/,
        include: path.join(__dirname, "src"),
        use: [
          MiniCssExtractPlugin.loader,
          {
            loader: "css-loader",
            options: {
              modules: {
                auto: true,
                exportLocalsConvention: "camelCase",
                localIdentName: "[local]_[hash:base64:3]"
              }
            }
          },
          "sass-loader"
        ]
      }
    ]
  },
  resolve: {
    extensions: [".tsx", ".ts", ".js"]
  },
  output: {
    path: outputPath,
    filename: "[name].js",
    library: {
      type: "module"
    },
    publicPath: "coui://ui-mods/"
  },
  optimization: {
    minimize: true,
    minimizer: [
      new TerserPlugin({
        extractComments: false
      })
    ]
  },
  experiments: {
    outputModule: true
  },
  plugins: [
    new MiniCssExtractPlugin({
      filename: "[name].css"
    }),
    new CopyManifestPlugin()
  ]
};
