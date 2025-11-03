// Copyright Epic Games, Inc. All Rights Reserved.

using UnrealBuildTool;

public class work : ModuleRules
{
	public work(ReadOnlyTargetRules Target) : base(Target)
	{
		PCHUsage = PCHUsageMode.UseExplicitOrSharedPCHs;

		PublicDependencyModuleNames.AddRange(new string[] { "Core", "CoreUObject", "Engine", "InputCore" });
	}
}
