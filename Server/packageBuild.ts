#!/usr/bin/env node
/**
 *
 * Credits: Dirtbikercj
 *
 */

const fs = require("fs-extra");
const glob = require("glob");
const zip = require("bestzip");
const path = require("path");
const chalk = require("chalk");
const processHost = require("child_process")

const { author, name:packageName, version } = require("./package.json");

const configText = fs.readFileSync("./build.json");
const config = JSON.parse(configText);
const currentDir = __dirname;

const modName = `${packageName.replace(/[^a-z0-9]/gi, "")}`;

const currentDate = new Date();
const epoch = `${currentDate}`

console.log(`[SPT build System] ${epoch}`);
if (config.GitStatus === true)
{
    if (fs.existsSync(`${currentDir}/.git`))
    {
        const gitStatus = processHost.execSync("git status -uno", {encoding: "utf-8"})
        console.log(gitStatus);
    }
    else
        console.log(chalk.yellow("[SPT Build System] GitStatus is enabled in build.json, but this isn't a Git repository."));
}
if (config.ShowBuildOptions)
{
    console.log("[SPT Build System] Build options:");
    console.log(`[SPT Build System] ContainPlugin:  ${config.ContainPlugin}`);
    console.log(`[SPT Build System] CopyBundles:    ${config.CopyBundles}`);
    console.log(`[SPT Build System] BuildZip:       ${config.BuildZip}`);
    if (config.ContainPlugin)
    {
        console.log(config.PluginFileName !== "" ?`[SPT Build System] PluginFileName: ${config.PluginFileName}`:chalk.red("[SPT Build System] PluginFileName: Not configured."));
        console.log(config.PluginPath !== "" ?`[SPT Build System] PluginPath:     ${config.PluginPath}`:chalk.red("[SPT Build System] PluginPath:     Not configured."));
    }
    console.log(`[SPT Build System] BuildDir:       ${config.BuildDir}`);
    console.log(`[SPT Build System] CopyToGame:     ${config.CopyToGame}`);
    console.log(`[SPT Build System] PathToGame:     ${config.PathToGame}`);
    console.log(`[SPT Build System] StartServer:    ${config.StartServer}`);
    console.log(`[SPT Build System] StartDelay:     ${config.StartDelay}`);
}
console.log(`[SPT Build System] Generated package name: ${modName}`);

if (config.FirstRun)
{
    // First run on new build system, remove dist folder.
    fs.rmSync(`${currentDir}\\dist`, { force: true, recursive: true });
    console.log(chalk.yellow("[SPT Build System] First time running npm build."));
    console.log(chalk.yellow("[SPT Build System] Please configure the build.json file."));
    console.log(chalk.yellow("[SPT Build System] Disable this warning by disabling FirstRun in \"build.json\"."));
    process.exit(1);
}


fs.rmSync(`${currentDir}\\${config.BuildDir}`, { force: true, recursive: true });
fs.rmSync(`${currentDir}\\${config.BuildDir}_zip`, { force: true, recursive: true });
console.log(chalk.green("[SPT Build System] Previous build files deleted."));

let exclude;
if (config.CopyBundles === true)
{
    exclude = glob.sync(`{${config.IgnoreList.join(",")}}`, { realpath: true, dot: true });
}
else
{
    config.IgnoreList.push("bundles/")
    exclude = glob.sync(`{${config.IgnoreList.join(",")}}`, { realpath: true, dot: true });
}

fs.copySync(currentDir, path.normalize(`${currentDir}/../~${modName}`), {filter: (src) =>
    {
        return !exclude.includes(src);
    }});

//temp folder path (only used with BuildZip enabled in build.json)
const tempPath = path.normalize(`${currentDir}/temp`)
const serverModPath = path.normalize(`${currentDir}/${config.BuildDir}/user/mods/${modName}`)
if (config.BuildZip)
{
    //Creating a temp folder with the mod
    fs.moveSync(path.normalize(`${currentDir}/../~${modName}`), tempPath, {overwrite: true});
    //Copying temp folder to output (build) folder with the proper structure
    fs.copySync(tempPath, serverModPath);
}
else
{
    //No need to create a temp folder
    fs.moveSync(path.normalize(`${currentDir}/../~${modName}`), serverModPath, {overwrite: true});
}


if (config.ContainPlugin)
{
    const pluginFolder = config.PluginPath !== ""?path.normalize(`${config.PluginPath}`):"Not configured.";
    if (fs.existsSync(pluginFolder) && config.PluginPath !== "")
        fs.copySync(path.normalize(`${pluginFolder}/${config.PluginFileName}`), path.normalize(`${currentDir}/${config.BuildDir}/BepInEx/plugins/${config.PluginFileName}`));
    else
        console.log(chalk.red(`[SPT Build System] Couldn't find your configured plugin folder: ${pluginFolder}`));
}

console.log(chalk.green(`[SPT Build System] Server files built. ${config.BuildDir} directory contains your newly built files.`));

if (config.CopyToGame)
{
    //const gamePath = path.normalize(`${config.PathToGame}`)
    if (fs.existsSync(config.PathToGame))
    {
        // Delete the existing build before we copy the new build over.
        fs.rmSync(`${config.PathToGame}/user/mods/${modName}`, {force: true, recursive: true});
        console.log(chalk.green("[SPT Build System] Files deleted in game directory."));

        // Copy files to the game directory
        fs.copySync(path.normalize(`${currentDir}/${config.BuildDir}`), config.PathToGame);
        console.log(chalk.green(`[SPT Build System] Files copied to game path: "${config.PathToGame}".`));
    }
    else
        console.log(chalk.red(`[SPT Build System] Copy failed, couldn't find your game folder at: "${config.PathToGame}".`));
}

if (config.BuildZip)
{
    // Create the temporary user/mod path
    const relativeZipPath = path.normalize(path.join(`${tempPath}/`, "user/", "mods/"));
    fs.mkdir(relativeZipPath, { recursive: true }, (err) =>
    {
        if (err)
        {
            console.error(chalk.red(`[SPT Build System] A error has occurred creating directory "${relativeZipPath}": `, err.stack), err);
        }
    });

    if (!fs.existsSync(path.normalize(`${currentDir}\\${config.BuildDir}_zip`)))
        fs.mkdirSync(path.normalize(`${currentDir}\\${config.BuildDir}_zip`));

    zip({
        source: "*",
        destination: `${currentDir}\\${config.BuildDir}_zip\\${modName}-${version ?? ""}.zip`,
        cwd: `${currentDir}/${config.BuildDir}`
    }).catch(function(err)
    {
        console.error(chalk.red("[SPT Build System] A bestzip error has occurred: ", err.stack));
    }).then(function()
    {
        // remove the temp directories
        fs.rmSync(tempPath, { force: true, recursive: true });
        console.log(chalk.green(`[SPT Build System] Compressed mod package to: ${config.BuildDir}_zip\\${modName}.zip`));
    });
}

if (config.StartServer === true && config.PathToGame !== "")
{
    if (fs.existsSync(config.PathToGame))
    {
        console.log(chalk.green(`[SPT Build System] Server starting in ${config.StartDelay} seconds.`));
        setTimeout(() =>
        {
            try
            {
                const command = "Spt.Server.Exe";
                const output = processHost.execSync(command, {
                    cwd: config.PathToGame,
                    stdio: "inherit" // This allows the output to be displayed in the terminal
                });
                console.log("Command executed successfully:", output.toString());
            }
            catch (error)
            {
                console.error("Error executing command:", error.message);
            }
        }, config.StartDelay * 1000);
    }
    else
    {
        console.log(chalk.red(`[SPT Build System] Server start failed, couldn't find your game folder at: "${config.PathToGame}".`));
    }
}
else
{
    if (config.StartServer === false)
        console.log(chalk.yellow("[SPT Build System] Server start is Disabled in config."));
    else if (config.PathToGame === "")
        console.log(chalk.red("[SPT Build System] Server start was ignored because \"PathToGame\" in \"build.json\" isn't configured."));
}