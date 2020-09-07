﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;
using Microsoft.Extensions.Logging;
using Rozetka.PriceTracker.Grpc.Features.GetProductInfo;
using Rozetka.PriceTracker.Grpc.Features.GetProducts;
using Rozetka.PriceTracker.Grpc.Features.LoadProduct;
using Rozetka.PriceTracker.Grpc.Features.TrackProduct;
using Rozetka.PriceTracker.Services.ProductLoader;
using Rozetka.PriceTracker.Services.Products;

namespace Rozetka.PriceTracker.Grpc.Services
{
    public class PriceTrackerService : PriceTracker.PriceTrackerBase
    {
        private readonly ILogger<PriceTrackerService> _logger;
        private readonly IProductLoaderService _productLoaderService;
        private readonly IProductsService _productsService;
        private readonly IMediator _mediator;

        public PriceTrackerService(ILogger<PriceTrackerService> logger,
            IProductLoaderService productLoaderService,
            IProductsService productsService,IMediator mediator)
        {
            _logger = logger;
            _productLoaderService = productLoaderService;
            _productsService = productsService;
            _mediator = mediator;
        }

        public override async Task<TrackProductResponse> TrackProduct(TrackProductRequest request, ServerCallContext context)
        {
            try
            {
                var product = await _mediator.Send(new LoadProductCommand() { ProductUrl = request.ProductUrl });

                if (product != null)
                {
                    return await _mediator.Send(new SaveProductCommand()
                    {
                        Product = product
                    });                    
                }
            }
            catch(Exception ex)
            {
                _logger.LogError($"An error has occurred when trying to add product for price tracking. {ex}");
            }

            return null;

        }

        public override async Task TrackProductStream(TrackProductRequest request, IServerStreamWriter<TrackProductResponse> responseStream, ServerCallContext context)
        {
            while (!context.CancellationToken.IsCancellationRequested)
            {
                var product = await _productLoaderService.LoadProductAsync(request.ProductUrl);

                if (product != null)
                {
                    await responseStream.WriteAsync(
                            new TrackProductResponse
                            {
                                Description = product.Description,
                                Discount = product.Discount,
                                Id = (int)product.Id,
                                ImageUrl = product.ImageUrl,
                                Price = (float)product.Price,
                                Title = product.Title,
                                Url = product.Href,
                                SellStatus = product.SellStatus,
                                Status = product.Status,
                            });


                }

                await Task.Delay(TimeSpan.FromSeconds(15));
            }

            if (context.CancellationToken.IsCancellationRequested)
            {
                Console.WriteLine("Stream canceled");
            }
        }

        public override async Task<TrackProductPriceResponse> TrackPrices(Empty request, ServerCallContext context)
        {
            try
            {
                return await _mediator.Send(new GetProducts.Query());
            }
            catch(Exception ex)
            {
                _logger.LogError($"An error has occurred when trying to get products to prices track. {ex}");
            }

            return null;

        }

        public override async Task<ProductInfoResponse> GetProductInfo(GetProductInfoRequest request, ServerCallContext context)
        {
            try
            {
                return await _mediator.Send(new GetProductInfo.Query() { ProductId = request.ProductId });
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error has occurred when trying to get product info. {ex}");
            }

            return null;
        }

    }
}
