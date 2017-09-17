while($true)
{
    [Collections.Generic.List[String]]$services =  [Collections.Generic.List[String]]::new()
    [Collections.Generic.Dictionary[String,Int]]$fds = [Collections.Generic.Dictionary[String,Int]]::new()

    $services.Add("fabric:/HealthMetrics/HealthMetrics.BandActorService")
    $services.Add("fabric:/HealthMetrics/HealthMetrics.DoctorActorService")
    $services.Add("fabric:/HealthMetrics/HealthMetrics.NationalService")
    $services.Add("fabric:/HealthMetrics/HealthMetrics.CountyService")

    $count = 0

    foreach($service in $services)
    {
        $primaries = Get-ServiceFabricApplication | Get-ServiceFabricService -ServiceName $service | Get-ServiceFabricPartition | Get-ServiceFabricReplica | Where-Object -Property "ReplicaRole" -eq "Primary"

        foreach($primary in $primaries)
        {
            $node = Get-ServiceFabricNode $primary.NodeName
            $uri = $node.FaultDomain.AbsoluteUri

            if($fds.ContainsKey("$uri"))
            {
                $fds["$uri"]++
            }
            else
            {
                $fds.Add("$uri", 1)
            }
        }
    }

    $fds | ft -AutoSize
}