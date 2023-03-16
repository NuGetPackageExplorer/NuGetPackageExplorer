using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Input;
using NupkgExplorer.Framework.MVVM;

namespace NupkgExplorer.Framework.Extensions
{
	public static class MvvmExtensions
	{
		public static IObservable<TProperty> WhenAnyValue<TViewModel, TProperty>(this TViewModel viewModel, Expression<Func<TViewModel, TProperty>> propertySelector)
			where TViewModel : ViewModelBase
		{
			var propertyName = GetPropertyName(propertySelector);
			var selector = propertySelector.Compile();

			return viewModel.ObservePropertyChanged()
				.Where(e => e.EventArgs.PropertyName == propertyName)
				.Select(_ => selector(viewModel));
		}

		public static IObservable<(TProperty1, TProperty2)> WhenAnyValue<TViewModel, TProperty1, TProperty2>(this TViewModel viewModel, Expression<Func<TViewModel, TProperty1>> propertySelector1, Expression<Func<TViewModel, TProperty2>> propertySelector2)
			where TViewModel : ViewModelBase
		{
			var propertyNames = new LambdaExpression[] { propertySelector1, propertySelector2 }
				.Select(GetPropertyName)
				.ToArray();
			var selector1 = propertySelector1.Compile();
			var selector2 = propertySelector2.Compile();

			return viewModel.ObservePropertyChanged()
				.Where(e => propertyNames.Contains(e.EventArgs.PropertyName))
				.Select(_ => (
					selector1(viewModel), 
					selector2(viewModel)
				));
		}

		public static void SubscribeToCommand<TDontCare>(this IObservable<TDontCare> source, ICommand command)
		{
			source.Subscribe(_ => command.Execute(default));
		}

		private static IObservable<EventPattern<PropertyChangedEventArgs>> ObservePropertyChanged(this ViewModelBase viewModel)
		{
			return Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>
			(
				h => viewModel.PropertyChanged += h,
				h => viewModel.PropertyChanged -= h
			);
		}

		private static string GetPropertyName(LambdaExpression propertySelector)
		{
			var member = propertySelector.Body as MemberExpression;
			var property = member.Member as PropertyInfo;

			return property.Name;
		}
	}
}
