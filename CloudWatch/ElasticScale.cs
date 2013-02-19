// -----------------------------------------------------------------------
// <copyright file="CloudWatch.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace Aws
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using Amazon;
    using Amazon.AutoScaling;
    using Amazon.AutoScaling.Model;
    using Amazon.CloudWatch;
    using Amazon.CloudWatch.Model;
    using Amazon.EC2;
    using Amazon.EC2.Model;
    using Amazon.S3;
    using Amazon.S3.Model;

    using NUnit.Framework;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    [TestFixture]
    public class ElasticScale
    {
        private const string LaunchConfigName = "my test launch config";
        private const string ScalingGroupName = "my test scale group";
        private const string IncreaseCapacityPolicyName = "test increase policy name";
        private const string DecreaseCapacityPolicyName = "test decrease policy name";
        private const string AlarmIncreaseCapacityName = "test alarm increase capacity name";
        private const string AlarmDecreaseCapacityName = "test alarm decrease capacity name";
        private const string AmiId = "ami-9bd2d3ef";
        private const string InstanceType = "t1.micro";
        private const string LoadBalancerName = "MyLoadBalancer";
        private readonly string[] AvailabilityZones = new[] { "eu-west-1a", "eu-west-1b", "eu-west-1c" };

        private AmazonAutoScaling scaleClient = AWSClientFactory.CreateAmazonAutoScalingClient(RegionEndpoint.EUWest1);
        private AmazonCloudWatch watchClient = AWSClientFactory.CreateAmazonCloudWatchClient(RegionEndpoint.EUWest1);

        private AmazonS3 s3Client = AWSClientFactory.CreateAmazonS3Client(RegionEndpoint.EUWest1);
        
        [Test]
        public void ConfigureScaling()
        {
            var launchConfig = this.scaleClient.CreateLaunchConfiguration(new CreateLaunchConfigurationRequest()
                                                                    .WithLaunchConfigurationName(LaunchConfigName)
                                                                    .WithImageId(AmiId)
                                                                    .WithInstanceType(InstanceType));

            var scalingGroup = this.scaleClient.CreateAutoScalingGroup(new CreateAutoScalingGroupRequest()
                                                                .WithAutoScalingGroupName(ScalingGroupName)
                                                                .WithMinSize(0)
                                                                .WithMaxSize(1)
                                                                .WithLoadBalancerNames(LoadBalancerName)
                                                                .WithAvailabilityZones(AvailabilityZones)
                                                                .WithLaunchConfigurationName(LaunchConfigName));
            var increaseCapacityScalingPolicy = this.scaleClient.PutScalingPolicy(new PutScalingPolicyRequest()
                                                            .WithAutoScalingGroupName(ScalingGroupName)
                                                            .WithPolicyName(IncreaseCapacityPolicyName)
                                                            .WithAdjustmentType("ChangeInCapacity")
                                                            .WithScalingAdjustment(1));

            var decreaseCapacityScalingPolicy = this.scaleClient.PutScalingPolicy(new PutScalingPolicyRequest()
                                                            .WithAutoScalingGroupName(ScalingGroupName)
                                                            .WithPolicyName(DecreaseCapacityPolicyName)
                                                            .WithAdjustmentType("ChangeInCapacity")
                                                            .WithScalingAdjustment(-1));

            var increaseCapacityAlarm = watchClient.PutMetricAlarm(new PutMetricAlarmRequest()
                                                .WithAlarmName(AlarmIncreaseCapacityName)
                                                .WithNamespace("AWS/ELB")
                                                .WithMetricName("RequestCount")
                                                .WithStatistic("Sum")
                                                .WithUnit("Count")
                                                .WithEvaluationPeriods(1)
                                                .WithComparisonOperator("GreaterThanOrEqualToThreshold")
                                                .WithPeriod(60)
                                                .WithThreshold(2)
                                                .WithAlarmActions(increaseCapacityScalingPolicy.PutScalingPolicyResult.PolicyARN));

            var decreaseCapacityAlarm = watchClient.PutMetricAlarm(new PutMetricAlarmRequest()
                                                .WithAlarmName(AlarmDecreaseCapacityName)
                                                .WithNamespace("AWS/ELB")
                                                .WithMetricName("RequestCount")
                                                .WithStatistic("Sum")
                                                .WithUnit("Count")
                                                .WithEvaluationPeriods(1)
                                                .WithComparisonOperator("LessThanOrEqualToThreshold")
                                                .WithPeriod(60)
                                                .WithThreshold(0)
                                                .WithAlarmActions(decreaseCapacityScalingPolicy.PutScalingPolicyResult.PolicyARN));

            ////var alarm = watchClient.PutMetricAlarm(new PutMetricAlarmRequest()
            ////                                                .WithAlarmName(AlarmName)
            ////                                                .WithNamespace(NameSpace)
            ////                                                .WithMetricName("CPUUtilization")
            ////                                                .WithStatistic("Average")
            ////                                                .WithUnit("Percent")
            ////                                                .WithEvaluationPeriods(1)
            ////                                                .WithComparisonOperator("GreaterThanOrEqualToThreshold")
            ////                                                .WithPeriod(60)
            ////                                                .WithThreshold(1)
            ////                                                .WithAlarmActions(scalingPolicy.PutScalingPolicyResult.PolicyARN));
        }

        [Test]
        public void Cleanup()
        {
            this.watchClient.DeleteAlarms(new DeleteAlarmsRequest().WithAlarmNames(AlarmIncreaseCapacityName));
            this.watchClient.DeleteAlarms(new DeleteAlarmsRequest().WithAlarmNames(AlarmDecreaseCapacityName));

            this.scaleClient.DeletePolicy(new DeletePolicyRequest().WithPolicyName(IncreaseCapacityPolicyName).WithAutoScalingGroupName(ScalingGroupName));
            this.scaleClient.DeletePolicy(new DeletePolicyRequest().WithPolicyName(DecreaseCapacityPolicyName).WithAutoScalingGroupName(ScalingGroupName));

            this.scaleClient.UpdateAutoScalingGroup(new UpdateAutoScalingGroupRequest().WithAutoScalingGroupName(ScalingGroupName).WithMinSize(0).WithMaxSize(0).WithLaunchConfigurationName(LaunchConfigName));
            var result = this.scaleClient.DeleteAutoScalingGroup(new DeleteAutoScalingGroupRequest().WithAutoScalingGroupName(ScalingGroupName).WithForceDelete(true));
            this.scaleClient.DeleteLaunchConfiguration(new DeleteLaunchConfigurationRequest().WithLaunchConfigurationName(LaunchConfigName));
        }
    }
}
