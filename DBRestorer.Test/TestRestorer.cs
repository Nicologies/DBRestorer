using System.IO;
using DBRestorer.Ctrl.Domain;
using ExtendedCL;
using NSubstitute;
using NUnit.Framework;

namespace DBRestorer.Test;

[TestFixture]
public class TestRestorer
{
    [Test]
    public void GivenACorruptDb_ShouldStopRestoring()
    {
        var progressBarProvider = Substitute.For<IProgressBarProvider>();
        var sqlUtil = Substitute.For<SqlServerUtilBase>();
        var opt = new SqlServerUtilBase.DbRestoreOptions();
        sqlUtil.When(x => x.Restore(opt, progressBarProvider, null))
            .Do(x => throw new InvalidDataException(""));

        var restorer = new Restorer(sqlUtil);

        Assert.Throws<InvalidDataException>(() => restorer.Restore(opt, progressBarProvider, null));
        progressBarProvider.DidNotReceive().Received(Arg.Any<int>());
    }
}