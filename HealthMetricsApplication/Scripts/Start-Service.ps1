$cloud = $false
$singleNode = $true
$constrainedNodeTypes = $false

$lowkey = "-9223372036854775808"
$highkey = "9223372036854775807" 

$countyLowKey = 0
$countyHighKey = 57000

$appName = "fabric:/HealthMetrics"
$appType = "HealthMetrics"
$appInitialVersion = "1.0.0.0"

if($singleNode)
{
    $webServiceInstanceCount = -1
    $bandCreationInstanceCount = -1
    $countyServicePartitionCount = 1
    $bandActorServicePartitionCount = 1
    $doctorServicePartitionCount = 1
}
else
{
    $webServiceInstanceCount = @{$true=-1;$false=1}[$cloud -eq $true] 
    $bandCreationInstanceCount = @{$true=-1;$false=1}[$cloud -eq $true] 
    $countyServicePartitionCount = @{$true=10;$false=5}[$cloud -eq $true]  
    $bandActorServicePartitionCount = @{$true=10;$false=5}[$cloud -eq $true]  
    $doctorServicePartitionCount = @{$true=10;$false=5}[$cloud -eq $true]  

    if($constrainedNodeTypes)
    {
        $webServiceConstraint = "NodeType == "
        $countyServiceConstraint = "NodeType == "
        $nationalServiceConstraint = "NodeType == "
        $bandServiceConstraint = "NodeType == "
        $doctorServiceConstraint = "NodeType == "   
        $bandCreationServiceConstraint = "NodeType == "        
    }
    else
    {
        $webServiceConstraint = ""
        $countyServiceConstraint = ""
        $nationalServiceConstraint = ""
        $bandServiceConstraint = ""
        $doctorServiceConstraint = ""
        $bandCreationServiceConstraint = ""   
    }
}

$webServiceType = "HealthMetrics.WebServiceType"
$webServiceName = "HealthMetrics.WebService"

$nationalServiceType = "HealthMetrics.NationalServiceType"
$nationalServiceName = "HealthMetrics.NationalService"
$nationalServiceReplicaCount = @{$true=1;$false=3}[$singleNode -eq $true]  

$countyServiceType = "HealthMetrics.CountyServiceType"
$countyServiceName = "HealthMetrics.CountyService"
$countyServiceReplicaCount = @{$true=1;$false=3}[$singleNode -eq $true]  

$bandCreationServiceType = "HealthMetrics.BandCreationServiceType"
$bandCreationServiceName = "HealthMetrics.BandCreationService"

$doctorServiceType = "HealthMetrics.DoctorServiceType"
$doctorServiceName = "HealthMetrics.DoctorService"
$doctorServiceReplicaCount = @{$true=1;$false=3}[$singleNode -eq $true]

$bandActorServiceType = "BandActorServiceType"
$bandActorServiceName= "HealthMetrics.BandActorService"
$bandActorReplicaCount = @{$true=1;$false=3}[$singleNode -eq $true]


New-ServiceFabricService -ServiceTypeName $webServiceType -Stateless -ApplicationName $appName -ServiceName "$appName/$webServiceName" -PartitionSchemeSingleton -InstanceCount $webServiceInstanceCount -PlacementConstraint $webServiceConstraint -ServicePackageActivationMode ExclusiveProcess

#create national
New-ServiceFabricService -ServiceTypeName $nationalServiceType -Stateful -HasPersistedState -ApplicationName $appName -ServiceName "$appName/$nationalServiceName" -PartitionSchemeSingleton -MinReplicaSetSize $nationalServiceReplicaCount -TargetReplicaSetSize $nationalServiceReplicaCount -PlacementConstraint $nationalServiceConstraint -ServicePackageActivationMode ExclusiveProcess

#create county
New-ServiceFabricService -ServiceTypeName $countyServiceType -Stateful -HasPersistedState -ApplicationName $appName -ServiceName "$appName/$countyServiceName" -PartitionSchemeUniformInt64 -LowKey $countyLowKey -HighKey $countyHighKey -PartitionCount $countyServicePartitionCount -MinReplicaSetSize $countyServiceReplicaCount -TargetReplicaSetSize $countyServiceReplicaCount -PlacementConstraint $countyServiceConstraint -ServicePackageActivationMode ExclusiveProcess

#create doctor
New-ServiceFabricService -ServiceTypeName $doctorServiceType -Stateful -HasPersistedState -ApplicationName $appName -ServiceName "$appName/$doctorServiceName" -PartitionSchemeUniformInt64 -LowKey $lowkey -HighKey $highkey -PartitionCount $doctorServicePartitionCount -MinReplicaSetSize $doctorServiceReplicaCount -TargetReplicaSetSize $doctorServiceReplicaCount -PlacementConstraint $doctorServiceConstraint -ServicePackageActivationMode ExclusiveProcess

#create band
New-ServiceFabricService -ServiceTypeName $bandActorServiceType -Stateful -ApplicationName $appName -ServiceName "$appName/$bandActorServiceName" -PartitionSchemeUniformInt64 -LowKey $lowkey -HighKey $highkey -PartitionCount $bandActorServicePartitionCount -MinReplicaSetSize $bandActorReplicaCount -TargetReplicaSetSize $bandActorReplicaCount -PlacementConstraint $bandServiceConstraint -ServicePackageActivationMode ExclusiveProcess

#create band creation
New-ServiceFabricService -ServiceTypeName $bandCreationServiceType -Stateless -ApplicationName $appName -ServiceName "$appName/$bandCreationServiceName" -PartitionSchemeSingleton -InstanceCount $bandCreationInstanceCount -PlacementConstraint $bandCreationServiceConstraint -ServicePackageActivationMode ExclusiveProcess